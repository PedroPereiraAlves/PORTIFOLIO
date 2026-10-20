using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Options;
using PedroHub.Infrastructure.Monitoramento;

namespace PedroHub.Application.Tests.Features.Monitoramento;

public sealed class ValidadorDestinoSeguroTests
{
    private static ValidadorDestinoSeguro Criar(IDnsResolver dns)
    {
        var opts = Options.Create(new MonitoramentoOptions { PortasPermitidas = [80, 443] });
        return new ValidadorDestinoSeguro(dns, opts);
    }

    [Fact]
    public async Task Validar_HttpsPublico_Aceita()
    {
        var dns = Substitute.For<IDnsResolver>();
        dns.ResolverAsync("site-rms-logistica.pages.dev", Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse("104.21.10.20")]);

        var result = await Criar(dns).ValidarAsync("https://site-rms-logistica.pages.dev/");

        result.EhSeguro.Should().BeTrue();
        result.UriNormalizada.Should().NotBeNull();
        result.IpsPermitidos.Should().ContainSingle(ip => ip.Equals(IPAddress.Parse("104.21.10.20")));
    }

    [Theory]
    [InlineData("ftp://example.com/")]
    [InlineData("http://localhost/")]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://10.0.0.5/")]
    [InlineData("http://169.254.169.254/")]
    [InlineData("http://[::1]/")]
    [InlineData("https://user:pass@example.com/")]
    public async Task Validar_DestinosInseguros_Rejeita(string url)
    {
        var dns = Substitute.For<IDnsResolver>();
        dns.ResolverAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var host = ci.ArgAt<string>(0);
                if (host is "localhost" or "127.0.0.1")
                    return Task.FromResult(new[] { IPAddress.Loopback });
                if (host == "10.0.0.5")
                    return Task.FromResult(new[] { IPAddress.Parse("10.0.0.5") });
                if (host == "169.254.169.254")
                    return Task.FromResult(new[] { IPAddress.Parse("169.254.169.254") });
                if (host is "::1" or "[::1]")
                    return Task.FromResult(new[] { IPAddress.IPv6Loopback });
                return Task.FromResult(new[] { IPAddress.Parse("1.2.3.4") });
            });

        var result = await Criar(dns).ValidarAsync(url);

        result.EhSeguro.Should().BeFalse();
        result.Motivo.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Validar_DnsResolveParaPrivado_Rejeita()
    {
        var dns = Substitute.For<IDnsResolver>();
        dns.ResolverAsync("evil.example", Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse("192.168.1.10")]);

        var result = await Criar(dns).ValidarAsync("https://evil.example/");

        result.EhSeguro.Should().BeFalse();
        result.Motivo.Should().Contain("bloqueado");
    }

    [Theory]
    [InlineData("::")]
    [InlineData("64:ff9b::a00:1")]
    [InlineData("2002:0a00:0001::")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("198.18.0.1")]
    public async Task Validar_DnsResolveParaFaixasBloqueadas_Rejeita(string ipTexto)
    {
        var dns = Substitute.For<IDnsResolver>();
        dns.ResolverAsync("evil.example", Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse(ipTexto)]);

        var result = await Criar(dns).ValidarAsync("https://evil.example/");

        result.EhSeguro.Should().BeFalse();
        result.Motivo.Should().Contain("bloqueado");
    }

    [Theory]
    [InlineData("::")]
    [InlineData("64:ff9b::a00:1")]
    [InlineData("2002:0a00:0001::")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::10.0.0.1")]
    [InlineData("198.18.0.1")]
    [InlineData("198.19.255.255")]
    [InlineData("192.0.0.1")]
    [InlineData("192.0.2.1")]
    [InlineData("198.51.100.1")]
    [InlineData("203.0.113.1")]
    [InlineData("255.255.255.255")]
    [InlineData("64:ff9b:1::1")]
    public void EhIpBloqueado_FaixasAdicionais_RetornaTrue(string ipTexto)
    {
        ValidadorDestinoSeguro.EhIpBloqueado(IPAddress.Parse(ipTexto)).Should().BeTrue();
    }

    [Fact]
    public void EhIpBloqueado_Ipv6AnyENone_Bloqueia()
    {
        ValidadorDestinoSeguro.EhIpBloqueado(IPAddress.IPv6Any).Should().BeTrue();
        ValidadorDestinoSeguro.EhIpBloqueado(IPAddress.IPv6None).Should().BeTrue();
    }

    [Fact]
    public void EhIpBloqueado_Ipv4Publico_Aceita()
    {
        ValidadorDestinoSeguro.EhIpBloqueado(IPAddress.Parse("104.21.10.20")).Should().BeFalse();
    }
}
