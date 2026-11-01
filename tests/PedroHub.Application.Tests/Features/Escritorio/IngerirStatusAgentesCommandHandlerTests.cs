using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Features.Escritorio.Commands;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Infrastructure.Persistence;

namespace PedroHub.Application.Tests.Features.Escritorio;

public sealed class IngerirStatusAgentesCommandHandlerTests
{
    private static ApplicationDbContext CriarDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<AgenteEscritorio> SeedAgenteAsync(ApplicationDbContext db, string slug = "front-hub")
    {
        var a = new AgenteEscritorio
        {
            Slug = slug,
            Nome = "Front Hub",
            Papel = "UI",
            Sala = SalaEscritorio.Hub,
            Ordem = 1,
            Ativo = true
        };
        db.AgentesEscritorio.Add(a);
        await db.SaveChangesAsync();
        return a;
    }

    [Fact]
    public async Task Handle_Upsert_GravaAtualEHistorico()
    {
        await using var db = CriarDb();
        var agente = await SeedAgenteAsync(db);
        var handler = new IngerirStatusAgentesCommandHandler(db, db);
        var quando = DateTime.UtcNow;

        var result = await handler.Handle(new IngerirStatusAgentesCommand(
        [
            new StatusAgenteIngestaoItem(
                "front-hub", "trabalhando", "Ajustes mobile", "Hub",
                "https://github.com/PedroPereiraAlves/PORTIFOLIO/pull/4", quando)
        ]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var atual = await db.StatusAgentesAtual.SingleAsync();
        atual.AgenteId.Should().Be(agente.Id);
        atual.Status.Should().Be(StatusAgenteEscritorio.Trabalhando);
        atual.TarefaAtual.Should().Be("Ajustes mobile");
        (await db.StatusAgentesHistorico.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_UpdateAntigo_Ignora()
    {
        await using var db = CriarDb();
        var agente = await SeedAgenteAsync(db);
        var handler = new IngerirStatusAgentesCommandHandler(db, db);
        var t1 = DateTime.UtcNow.AddMinutes(-10);
        var t0 = t1.AddMinutes(-5);

        await handler.Handle(new IngerirStatusAgentesCommand(
        [
            new StatusAgenteIngestaoItem("front-hub", "trabalhando", "novo", "Hub", null, t1)
        ]), CancellationToken.None);

        await handler.Handle(new IngerirStatusAgentesCommand(
        [
            new StatusAgenteIngestaoItem("front-hub", "ocioso", "velho", "Hub", null, t0)
        ]), CancellationToken.None);

        var atual = await db.StatusAgentesAtual.SingleAsync(s => s.AgenteId == agente.Id);
        atual.Status.Should().Be(StatusAgenteEscritorio.Trabalhando);
        atual.TarefaAtual.Should().Be("novo");
        (await db.StatusAgentesHistorico.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_SlugInexistente_Falha()
    {
        await using var db = CriarDb();
        await SeedAgenteAsync(db);
        var handler = new IngerirStatusAgentesCommandHandler(db, db);

        var result = await handler.Handle(new IngerirStatusAgentesCommand(
        [
            new StatusAgenteIngestaoItem("nao-existe", "ocioso", null, null, null, DateTime.UtcNow)
        ]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("slug");
    }
}
