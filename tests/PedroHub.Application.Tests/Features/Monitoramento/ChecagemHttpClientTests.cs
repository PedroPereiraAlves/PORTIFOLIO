using System.Net;
using FluentAssertions;
using NSubstitute;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Domain.Enums;
using PedroHub.Infrastructure.Monitoramento;

namespace PedroHub.Application.Tests.Features.Monitoramento;

public sealed class ChecagemHttpClientTests
{
    private static ChecagemHttpClient Criar(
        HttpMessageHandler handler,
        IValidadorDestinoSeguro? validador = null)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(1) };
        factory.CreateClient(ChecagemHttpClient.HttpClientName).Returns(client);

        validador ??= Substitute.For<IValidadorDestinoSeguro>();
        return new ChecagemHttpClient(factory, validador);
    }

    [Fact]
    public async Task Executar_200_RetornaSucessoComLatencia()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = Criar(handler);

        var result = await client.ExecutarAsync(
            new Uri("https://site-rms-logistica.pages.dev/"),
            MetodoChecagem.Get,
            5000,
            [IPAddress.Parse("1.2.3.4")]);

        result.SucessoHttp.Should().BeTrue();
        result.CodigoHttp.Should().Be(200);
        result.TimeoutOuErroRede.Should().BeFalse();
        result.LatenciaMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Executar_500_RetornaCodigo()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = Criar(handler);

        var result = await client.ExecutarAsync(
            new Uri("https://example.com/"),
            MetodoChecagem.Get,
            5000,
            [IPAddress.Parse("1.2.3.4")]);

        result.SucessoHttp.Should().BeTrue();
        result.CodigoHttp.Should().Be(500);
    }

    [Fact]
    public async Task Executar_Timeout_MarcaErroRede()
    {
        var handler = new StubHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = Criar(handler);

        var result = await client.ExecutarAsync(
            new Uri("https://example.com/"),
            MetodoChecagem.Get,
            timeoutMs: 50,
            [IPAddress.Parse("1.2.3.4")]);

        result.TimeoutOuErroRede.Should().BeTrue();
        result.Erro.Should().Contain("Timeout");
    }

    [Fact]
    public async Task Executar_RedirectParaIpPrivado_Bloqueia()
    {
        var validador = Substitute.For<IValidadorDestinoSeguro>();
        validador.ValidarAsync("http://10.0.0.5/", Arg.Any<CancellationToken>())
            .Returns(new ResultadoValidacaoDestino(false, "IP resolvido bloqueado por política SSRF (10.0.0.5).", null, null));

        var handler = new StubHandler(_ =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.Found);
            resp.Headers.Location = new Uri("http://10.0.0.5/");
            return resp;
        });

        var client = Criar(handler, validador);
        var result = await client.ExecutarAsync(
            new Uri("https://example.com/"),
            MetodoChecagem.Get,
            5000,
            [IPAddress.Parse("1.2.3.4")]);

        result.TimeoutOuErroRede.Should().BeTrue();
        result.Erro.Should().Contain("bloqueado");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _fn;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> fn)
            => _fn = (r, _) => Task.FromResult(fn(r));

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> fn) => _fn = fn;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _fn(request, cancellationToken);
    }
}
