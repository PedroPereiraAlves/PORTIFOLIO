using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Domain.Enums;
using PedroHub.Domain.Monitoramento;

namespace PedroHub.Infrastructure.Monitoramento;

public sealed class ChecagemHttpClient : IChecagemHttpClient
{
    public const string HttpClientName = "monitoramento";
    private const int MaxRedirects = 3;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IValidadorDestinoSeguro _validador;

    public ChecagemHttpClient(IHttpClientFactory httpClientFactory, IValidadorDestinoSeguro validador)
    {
        _httpClientFactory = httpClientFactory;
        _validador = validador;
    }

    public async Task<ResultadoChecagemHttp> ExecutarAsync(
        Uri url,
        MetodoChecagem metodo,
        int timeoutMs,
        IReadOnlyList<IPAddress> ipsPermitidos,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeoutMs);

            var client = _httpClientFactory.CreateClient(HttpClientName);
            var atual = url;
            var ips = ipsPermitidos;

            for (var hop = 0; hop <= MaxRedirects; hop++)
            {
                using var request = new HttpRequestMessage(
                    metodo == MetodoChecagem.Head ? HttpMethod.Head : HttpMethod.Get,
                    atual);

                request.Options.Set(new HttpRequestOptionsKey<IReadOnlyList<IPAddress>>("AllowedIps"), ips);

                using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);

                var codigo = (int)response.StatusCode;
                if (EhRedirect(codigo) && response.Headers.Location is not null)
                {
                    if (hop == MaxRedirects)
                    {
                        sw.Stop();
                        return new ResultadoChecagemHttp(
                            false, codigo, (int)sw.ElapsedMilliseconds, true,
                            "Limite de redirects excedido.");
                    }

                    var proxima = response.Headers.Location.IsAbsoluteUri
                        ? response.Headers.Location
                        : new Uri(atual, response.Headers.Location);

                    var validacao = await _validador.ValidarAsync(proxima.AbsoluteUri, timeoutCts.Token);
                    if (!validacao.EhSeguro || validacao.UriNormalizada is null || validacao.IpsPermitidos is null)
                    {
                        sw.Stop();
                        return new ResultadoChecagemHttp(
                            false, codigo, (int)sw.ElapsedMilliseconds, true,
                            validacao.Motivo ?? "Redirect para destino inseguro.");
                    }

                    atual = validacao.UriNormalizada;
                    ips = validacao.IpsPermitidos;
                    continue;
                }

                sw.Stop();
                return new ResultadoChecagemHttp(
                    SucessoHttp: true,
                    CodigoHttp: codigo,
                    LatenciaMs: (int)sw.ElapsedMilliseconds,
                    TimeoutOuErroRede: false,
                    Erro: null);
            }

            sw.Stop();
            return new ResultadoChecagemHttp(false, null, (int)sw.ElapsedMilliseconds, true, "Falha após redirects.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            return new ResultadoChecagemHttp(false, null, (int)sw.ElapsedMilliseconds, true, "Timeout da checagem.");
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            return new ResultadoChecagemHttp(false, null, (int)sw.ElapsedMilliseconds, true, Truncar(ex.Message));
        }
        catch (SocketException ex)
        {
            sw.Stop();
            return new ResultadoChecagemHttp(false, null, (int)sw.ElapsedMilliseconds, true, Truncar(ex.Message));
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ResultadoChecagemHttp(false, null, (int)sw.ElapsedMilliseconds, true, Truncar(ex.Message));
        }
    }

    private static bool EhRedirect(int codigo) =>
        codigo is 301 or 302 or 303 or 307 or 308;

    private static string Truncar(string texto) =>
        texto.Length <= 500 ? texto : texto[..500];
}
