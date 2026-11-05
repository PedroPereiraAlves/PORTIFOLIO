using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Options;
using PedroHub.Application.Features.CursorConsumo.Commands;
using PedroHub.Application.Features.CursorConsumo.Queries;
using PedroHub.Domain.CursorConsumo;
using PedroHub.Infrastructure.Persistence;

namespace PedroHub.Application.Tests.Features.CursorConsumo;

public sealed class ImportarCsvUsoCursorCommandHandlerTests
{
    private const string Csv =
        "Date,User,Model,Total Tokens,Cost\n" +
        "2026-11-04T10:00:00Z,pedro@exemplo.com,grok,1000,$1.00\n" +
        "2026-11-04T10:00:00Z,pedro@exemplo.com,grok,1000,$1.00\n" +
        "2026-11-05T10:00:00Z,pedro@exemplo.com,opus,250,-\n";

    [Fact]
    public async Task Importa_E_ReimportarNaoDuplica()
    {
        await using var db = CriarDb();
        var handler = new ImportarCsvUsoCursorCommandHandler(db);

        var primeira = await handler.Handle(new ImportarCsvUsoCursorCommand(Csv, "uso.csv"), CancellationToken.None);
        var segunda = await handler.Handle(new ImportarCsvUsoCursorCommand(Csv, "uso.csv"), CancellationToken.None);

        primeira.IsSuccess.Should().BeTrue();
        primeira.Value.Inseridos.Should().Be(2);
        primeira.Value.Duplicados.Should().Be(1);
        segunda.Value.Inseridos.Should().Be(0);
        segunda.Value.Duplicados.Should().Be(3);
        (await db.EventosUsoCursor.CountAsync()).Should().Be(2);
        (await db.ImportacoesUsoCursor.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task CsvSemData_FalhaSemGravar()
    {
        await using var db = CriarDb();
        var handler = new ImportarCsvUsoCursorCommandHandler(db);

        var resultado = await handler.Handle(
            new ImportarCsvUsoCursorCommand("Model,Total Tokens\ngrok,1\n", "ruim.csv"),
            CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        (await db.EventosUsoCursor.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void Validador_RejeitaVazio()
    {
        var validator = new ImportarCsvUsoCursorCommandValidator();
        var result = validator.TestValidate(new ImportarCsvUsoCursorCommand("", null));
        result.ShouldHaveValidationErrorFor(x => x.Conteudo);
    }

    private static ApplicationDbContext CriarDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}

public sealed class SincronizarUsoCursorApiCommandHandlerTests
{
    [Fact]
    public async Task SemChave_NaoGrava()
    {
        await using var db = CriarDb();
        var client = Substitute.For<ICursorAdminClient>();
        client.EstaConfigurado.Returns(false);
        var handler = new SincronizarUsoCursorApiCommandHandler(db, client);

        var resultado = await handler.Handle(new SincronizarUsoCursorApiCommand(7), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await client.DidNotReceive().BuscarAsync(
            Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        (await db.EventosUsoCursor.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ComEventos_GravaUmaVez()
    {
        await using var db = CriarDb();
        var client = Substitute.For<ICursorAdminClient>();
        client.EstaConfigurado.Returns(true);
        var evento = new EventoUsoNormalizado(
            new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc),
            "pedro@exemplo.com",
            "grok",
            "Usage-based",
            false,
            10,
            1,
            2,
            3,
            16,
            21.36m,
            1m);
        client.BuscarAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(PedroHub.Shared.Results.Result.Success(new LeituraEventosCursor([evento], false)));

        var handler = new SincronizarUsoCursorApiCommandHandler(db, client);
        var resultado = await handler.Handle(new SincronizarUsoCursorApiCommand(1), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Inseridos.Should().Be(1);
        (await db.EventosUsoCursor.SingleAsync()).Origem.Should().Be("api");
    }

    [Fact]
    public void Validador_LimitaANoventaDias()
    {
        var validator = new SincronizarUsoCursorApiCommandValidator();
        validator.TestValidate(new SincronizarUsoCursorApiCommand(91)).ShouldHaveValidationErrorFor(x => x.Dias);
        validator.TestValidate(new SincronizarUsoCursorApiCommand(30)).ShouldNotHaveAnyValidationErrors();
    }

    private static ApplicationDbContext CriarDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}

public sealed class ObterConsumoCursorQueryHandlerTests
{
    [Fact]
    public async Task Periodo_SomaSoOQueCaiNaJanela_E_MostraCota()
    {
        await using var db = CriarDb();
        db.EventosUsoCursor.Add(new Domain.Entities.EventoUsoCursor
        {
            OcorridoEm = DateTime.UtcNow,
            Modelo = "grok",
            TokensTotal = 100,
            TokensEntrada = 100,
            CustoCentavos = 50m,
            Origem = "csv",
            ChaveNatural = "a",
            CriadoEm = DateTime.UtcNow
        });
        db.EventosUsoCursor.Add(new Domain.Entities.EventoUsoCursor
        {
            OcorridoEm = DateTime.UtcNow.AddDays(-40),
            Modelo = "opus",
            TokensTotal = 999,
            Origem = "csv",
            ChaveNatural = "b",
            CriadoEm = DateTime.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var handler = new ObterConsumoCursorQueryHandler(db, Options.Create(new CursorConsumoOptions
        {
            CotaTokens = 1000,
            CotaUsd = 10,
            DiaInicioCiclo = 1,
            ApiKey = "nao-logar"
        }));

        var resultado = await handler.Handle(new ObterConsumoCursorQuery("7d"), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Requisicoes.Should().Be(1);
        resultado.Value.Tokens.Should().Be(100);
        resultado.Value.CustoCentavos.Should().Be(50m);
        resultado.Value.PercentualCotaTokens.Should().Be(10.0m);
        resultado.Value.ApiConfigurada.Should().BeTrue();
        resultado.Value.PorDia.Should().NotBeEmpty();
        resultado.Value.PorModelo.Should().ContainSingle(m => m.Modelo == "grok");
    }

    private static ApplicationDbContext CriarDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
