using System.Net;
using System.Net.Sockets;

namespace PedroHub.Infrastructure.Monitoramento;

public static class SsrfSocketsHandlerFactory
{
    public static SocketsHttpHandler Create()
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseProxy = false,
            ConnectCallback = async (context, cancellationToken) =>
            {
                if (!context.InitialRequestMessage.Options.TryGetValue(
                        new HttpRequestOptionsKey<IReadOnlyList<IPAddress>>("AllowedIps"),
                        out var allowed)
                    || allowed is null
                    || allowed.Count == 0)
                {
                    throw new HttpRequestException("Conexão bloqueada: IPs permitidos ausentes.");
                }

                Exception? last = null;
                foreach (var ip in allowed)
                {
                    if (ValidadorDestinoSeguro.EhIpBloqueado(ip))
                        continue;

                    var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(ip, context.DnsEndPoint.Port, cancellationToken);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch (Exception ex)
                    {
                        socket.Dispose();
                        last = ex;
                    }
                }

                throw new HttpRequestException(
                    "Não foi possível conectar a um IP permitido.",
                    last);
            }
        };
    }
}
