using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PedroHub.Application.Common.Options;
using PedroHub.Domain.Enums;
using PedroHub.Infrastructure.Alertas;

namespace PedroHub.Application.Tests.Features.Monitoramento;

public sealed class ResendAlertaServiceTests
{
    private static ResendAlertaService Criar(
        HttpMessageHandler handler,
        string? apiKey,
        int minutosPorAlvo = 10,
        int maxHora = 20)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("resend").Returns(new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") });

        return new ResendAlertaService(
            factory,
            Options.Create(new ResendOptions { ApiKey = apiKey }),
            Options.Create(new MonitoramentoOptions
            {
                Alertas = new AlertasOptions
                {
                    De = "alertas@test.local",
                    Para = "owner@test.local",
                    MinutosEntreEmailsPorAlvo = minutosPorAlvo,
                    MaxEmailsPorHora = maxHora
                }
            }),
            NullLogger<ResendAlertaService>.Instance);
    }

    [Fact]
    public async Task SemApiKey_NaoQuebraNemEnvia()
    {
        var handler = new CountingHandler();
        var svc = Criar(handler, apiKey: null);

        await svc.NotificarMudancaStatusAsync(
            Guid.NewGuid(), "alvo", "https://ex.com",
            StatusAlvo.Online, StatusAlvo.Offline);

        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ComApiKey_EnviaUmaVez()
    {
        var handler = new CountingHandler { Status = HttpStatusCode.OK };
        var svc = Criar(handler, apiKey: "re_test");

        await svc.NotificarMudancaStatusAsync(
            Guid.NewGuid(), "alvo", "https://ex.com",
            StatusAlvo.Online, StatusAlvo.Offline);

        handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task RateLimitPorAlvo_BloqueiaSegundoEnvio()
    {
        var handler = new CountingHandler { Status = HttpStatusCode.OK };
        var svc = Criar(handler, apiKey: "re_test", minutosPorAlvo: 10);
        var id = Guid.NewGuid();

        await svc.NotificarMudancaStatusAsync(id, "alvo", "https://ex.com", StatusAlvo.Online, StatusAlvo.Offline);
        await svc.NotificarMudancaStatusAsync(id, "alvo", "https://ex.com", StatusAlvo.Offline, StatusAlvo.Online);

        handler.Calls.Should().Be(1);
    }

    [Fact]
    public void MascararEmail_OcultaMeioDoUsuario()
    {
        ResendAlertaService.MascararEmail("pedro@example.com").Should().Be("p***o@example.com");
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(Status));
        }
    }
}
