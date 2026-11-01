using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Options;
using PedroHub.Application.Features.Escritorio.Commands;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Infrastructure.Persistence;

namespace PedroHub.Application.Tests.Features.Escritorio;

public sealed class AplicarRetencaoEscritorioCommandHandlerTests
{
    private static ApplicationDbContext CriarDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_RemoveSoOQuePassouDoCorte()
    {
        await using var db = CriarDb();
        var agente = new AgenteEscritorio
        {
            Slug = "gideon",
            Nome = "Gideon",
            Papel = "TL",
            Sala = SalaEscritorio.Central,
            Ativo = true
        };
        db.AgentesEscritorio.Add(agente);
        await db.SaveChangesAsync();

        db.StatusAgentesHistorico.Add(new StatusAgenteHistorico
        {
            AgenteId = agente.Id,
            Status = StatusAgenteEscritorio.Ocioso,
            PublicadoEm = DateTime.UtcNow.AddDays(-100)
        });
        db.StatusAgentesHistorico.Add(new StatusAgenteHistorico
        {
            AgenteId = agente.Id,
            Status = StatusAgenteEscritorio.Trabalhando,
            PublicadoEm = DateTime.UtcNow.AddDays(-1)
        });
        db.EventosAgente.Add(new EventoAgente
        {
            AgenteId = agente.Id,
            Tipo = TipoEventoAgente.Entrega,
            Titulo = "velho",
            OcorridoEm = DateTime.UtcNow.AddDays(-120)
        });
        db.EventosAgente.Add(new EventoAgente
        {
            AgenteId = agente.Id,
            Tipo = TipoEventoAgente.Entrega,
            Titulo = "novo",
            OcorridoEm = DateTime.UtcNow.AddDays(-2)
        });
        await db.SaveChangesAsync();

        var handler = new AplicarRetencaoEscritorioCommandHandler(
            db,
            db,
            Options.Create(new EscritorioOptions { RetencaoDias = 90 }),
            NullLogger<AplicarRetencaoEscritorioCommandHandler>.Instance);

        var result = await handler.Handle(new AplicarRetencaoEscritorioCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
        (await db.StatusAgentesHistorico.CountAsync()).Should().Be(1);
        (await db.EventosAgente.SingleAsync()).Titulo.Should().Be("novo");
    }
}
