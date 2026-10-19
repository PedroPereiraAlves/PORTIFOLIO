using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Options;
using PedroHub.Application.Features.Monitoramento.Commands;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Domain.Monitoramento;
using PedroHub.Infrastructure.Persistence;
using System.Net;

namespace PedroHub.Application.Tests.Features.Monitoramento;

public sealed class ExecutarChecagemCommandHandlerTests
{
    private static ApplicationDbContext CriarDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static AlvoMonitorado CriarAlvo(StatusAlvo status = StatusAlvo.Online, int falhas = 0) =>
        new()
        {
            Nome = "SITE-RMS",
            Url = "https://site-rms-logistica.pages.dev",
            Metodo = MetodoChecagem.Get,
            IntervaloSegundos = 300,
            TimeoutMs = 10_000,
            StatusEsperado = "200",
            LimiarDegradadoMs = 1500,
            Ativo = true,
            StatusAtual = status,
            FalhasConsecutivas = falhas,
            ProximaChecagemEm = DateTime.UtcNow
        };

    private static ExecutarChecagemCommandHandler CriarHandler(
        ApplicationDbContext db,
        IChecagemHttpClient http,
        IAlertaService? alertas = null,
        IValidadorDestinoSeguro? validador = null)
    {
        validador ??= Substitute.For<IValidadorDestinoSeguro>();
        validador.ValidarAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ResultadoValidacaoDestino(
                true, null, new Uri("https://site-rms-logistica.pages.dev/"),
                [IPAddress.Parse("104.21.10.20")]));

        alertas ??= Substitute.For<IAlertaService>();

        return new ExecutarChecagemCommandHandler(
            db, db, validador, http, alertas,
            Options.Create(new MonitoramentoOptions { FalhasParaOffline = 2 }),
            NullLogger<ExecutarChecagemCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_200Rapido_Online()
    {
        await using var db = CriarDb();
        var alvo = CriarAlvo(StatusAlvo.Desconhecido);
        db.AlvosMonitorados.Add(alvo);
        await db.SaveChangesAsync();

        var http = Substitute.For<IChecagemHttpClient>();
        http.ExecutarAsync(Arg.Any<Uri>(), Arg.Any<MetodoChecagem>(), Arg.Any<int>(),
                Arg.Any<IReadOnlyList<IPAddress>>(), Arg.Any<CancellationToken>())
            .Returns(new ResultadoChecagemHttp(true, 200, 120, false, null));

        var alertas = Substitute.For<IAlertaService>();
        var handler = CriarHandler(db, http, alertas);

        var result = await handler.Handle(new ExecutarChecagemCommand(alvo.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(StatusAlvo.Online);
        result.Value.LatenciaMs.Should().Be(120);
        result.Value.CodigoHttp.Should().Be(200);
        (await db.AlvosMonitorados.SingleAsync()).StatusAtual.Should().Be(StatusAlvo.Online);
        await alertas.DidNotReceiveWithAnyArgs()
            .NotificarMudancaStatusAsync(default, default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_200Lento_Degradado()
    {
        await using var db = CriarDb();
        var alvo = CriarAlvo();
        db.AlvosMonitorados.Add(alvo);
        await db.SaveChangesAsync();

        var http = Substitute.For<IChecagemHttpClient>();
        http.ExecutarAsync(Arg.Any<Uri>(), Arg.Any<MetodoChecagem>(), Arg.Any<int>(),
                Arg.Any<IReadOnlyList<IPAddress>>(), Arg.Any<CancellationToken>())
            .Returns(new ResultadoChecagemHttp(true, 200, 2500, false, null));

        var handler = CriarHandler(db, http);
        var result = await handler.Handle(new ExecutarChecagemCommand(alvo.Id), CancellationToken.None);

        result.Value.Status.Should().Be(StatusAlvo.Degradado);
    }

    [Fact]
    public async Task Handle_500_PrimeiraFalha_Degradado()
    {
        await using var db = CriarDb();
        var alvo = CriarAlvo(StatusAlvo.Online, falhas: 0);
        db.AlvosMonitorados.Add(alvo);
        await db.SaveChangesAsync();

        var http = Substitute.For<IChecagemHttpClient>();
        http.ExecutarAsync(Arg.Any<Uri>(), Arg.Any<MetodoChecagem>(), Arg.Any<int>(),
                Arg.Any<IReadOnlyList<IPAddress>>(), Arg.Any<CancellationToken>())
            .Returns(new ResultadoChecagemHttp(true, 500, 80, false, null));

        var alertas = Substitute.For<IAlertaService>();
        var handler = CriarHandler(db, http, alertas);
        var result = await handler.Handle(new ExecutarChecagemCommand(alvo.Id), CancellationToken.None);

        result.Value.Status.Should().Be(StatusAlvo.Degradado);
        await alertas.Received(1).NotificarMudancaStatusAsync(
            alvo.Id, alvo.Nome, Arg.Any<string>(), StatusAlvo.Online, StatusAlvo.Degradado,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Timeout_SegundaFalha_Offline()
    {
        await using var db = CriarDb();
        var alvo = CriarAlvo(StatusAlvo.Degradado, falhas: 1);
        db.AlvosMonitorados.Add(alvo);
        await db.SaveChangesAsync();

        var http = Substitute.For<IChecagemHttpClient>();
        http.ExecutarAsync(Arg.Any<Uri>(), Arg.Any<MetodoChecagem>(), Arg.Any<int>(),
                Arg.Any<IReadOnlyList<IPAddress>>(), Arg.Any<CancellationToken>())
            .Returns(new ResultadoChecagemHttp(false, null, 10000, true, "Timeout da checagem."));

        var handler = CriarHandler(db, http);
        var result = await handler.Handle(new ExecutarChecagemCommand(alvo.Id), CancellationToken.None);

        result.Value.Status.Should().Be(StatusAlvo.Offline);
        result.Value.Erro.Should().Contain("Timeout");
    }
}
