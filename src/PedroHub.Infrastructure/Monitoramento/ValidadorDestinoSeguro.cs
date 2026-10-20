using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Options;

namespace PedroHub.Infrastructure.Monitoramento;

public sealed class ValidadorDestinoSeguro : IValidadorDestinoSeguro
{
    private readonly IDnsResolver _dns;
    private readonly MonitoramentoOptions _options;

    public ValidadorDestinoSeguro(IDnsResolver dns, IOptions<MonitoramentoOptions> options)
    {
        _dns = dns;
        _options = options.Value;
    }

    public async Task<ResultadoValidacaoDestino> ValidarAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Falha("URL vazia.");

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return Falha("URL inválida.");

        if (uri.Scheme is not ("http" or "https"))
            return Falha("Apenas http e https são permitidos.");

        if (!string.IsNullOrEmpty(uri.UserInfo))
            return Falha("URL não pode conter usuário ou senha.");

        var portas = _options.PortasPermitidas is { Length: > 0 }
            ? _options.PortasPermitidas
            : [80, 443];

        var porta = uri.IsDefaultPort
            ? (uri.Scheme == Uri.UriSchemeHttps ? 443 : 80)
            : uri.Port;

        if (!portas.Contains(porta))
            return Falha($"Porta {porta} não permitida.");

        IPAddress[] ips;
        try
        {
            ips = await _dns.ResolverAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch (Exception)
        {
            return Falha("Não foi possível resolver o DNS do host.");
        }

        if (ips.Length == 0)
            return Falha("DNS não retornou endereços.");

        foreach (var ip in ips)
        {
            if (EhIpBloqueado(ip))
                return Falha($"IP resolvido bloqueado por política SSRF ({ip}).");
        }

        var builder = new UriBuilder(uri) { UserName = "", Password = "" };
        return new ResultadoValidacaoDestino(true, null, builder.Uri, ips);
    }

    public static bool EhIpBloqueado(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        if (ip.AddressFamily == AddressFamily.InterNetwork)
            return EhIpv4Bloqueado(ip);

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return EhIpv6Bloqueado(ip);

        return false;
    }

    private static bool EhIpv4Bloqueado(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        // 0.0.0.0/8
        if (bytes[0] == 0) return true;
        // 10.0.0.0/8
        if (bytes[0] == 10) return true;
        // 127.0.0.0/8
        if (bytes[0] == 127) return true;
        // 169.254.0.0/16 (link-local + metadata)
        if (bytes[0] == 169 && bytes[1] == 254) return true;
        // 172.16.0.0/12
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
        // 192.168.0.0/16
        if (bytes[0] == 192 && bytes[1] == 168) return true;
        // 100.64.0.0/10 CGNAT
        if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return true;
        // 192.0.0.0/24 (IETF Protocol Assignments)
        if (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0) return true;
        // 192.0.2.0/24 TEST-NET-1
        if (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2) return true;
        // 198.18.0.0/15 benchmarking
        if (bytes[0] == 198 && bytes[1] is 18 or 19) return true;
        // 198.51.100.0/24 TEST-NET-2
        if (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100) return true;
        // 203.0.113.0/24 TEST-NET-3
        if (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113) return true;
        // 255.255.255.255 e multicast/reservado 224.0.0.0/4 (+ 240.0.0.0/4)
        if (bytes[0] >= 224) return true;
        return false;
    }

    private static bool EhIpv6Bloqueado(IPAddress ip)
    {
        if (IPAddress.IPv6Loopback.Equals(ip))
            return true;

        // :: (unspecified) — no Linux, conectar em [::] cai no próprio host
        if (IPAddress.IPv6Any.Equals(ip) || IPAddress.IPv6None.Equals(ip))
            return true;

        var b = ip.GetAddressBytes();

        // IPv4-compatible ::a.b.c.d (deprecated): primeiros 96 bits zero
        if (EhPrefixoZero(b, 12))
            return true;

        // NAT64 well-known prefix 64:ff9b::/96 — hextets 0064:ff9b:…; IPv4 nos últimos 32 bits
        if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b
            && EhPrefixoZero(b.AsSpan(4, 8)))
        {
            var embutido = new IPAddress(new[] { b[12], b[13], b[14], b[15] });
            return EhIpv4Bloqueado(embutido);
        }

        // Prefixo local NAT64 64:ff9b:1::/48
        if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b
            && b[4] == 0x00 && b[5] == 0x01)
        {
            return true;
        }

        // 6to4 2002::/16 — IPv4 nos bytes 2–5
        if (b[0] == 0x20 && b[1] == 0x02)
        {
            var embutido = new IPAddress(new[] { b[2], b[3], b[4], b[5] });
            return EhIpv4Bloqueado(embutido);
        }

        // fe80::/10 link-local
        if (b[0] == 0xfe && (b[1] & 0xc0) == 0x80)
            return true;

        // fc00::/7 unique local
        if ((b[0] & 0xfe) == 0xfc)
            return true;

        if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast)
            return true;

        return false;
    }

    private static bool EhPrefixoZero(byte[] bytes, int length)
    {
        for (var i = 0; i < length; i++)
        {
            if (bytes[i] != 0)
                return false;
        }

        return true;
    }

    private static bool EhPrefixoZero(ReadOnlySpan<byte> bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != 0)
                return false;
        }

        return true;
    }

    private static ResultadoValidacaoDestino Falha(string motivo) =>
        new(false, motivo, null, null);
}
