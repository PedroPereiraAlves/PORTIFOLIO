using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Options;
using PedroHub.Application.Features.Escritorio.Queries;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Infrastructure.Persistence;

namespace PedroHub.Application.Tests.Features.Escritorio;

public sealed class ObterEstadoEscritorioQueryTests
{
    private static ApplicationDbContext CriarDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_MapeiaStatusEDesatualizado_ELimitaEventos()
    {
        await using var db = CriarDb();
        var agente = new AgenteEscritorio
        {
            Slug = "gideon",
            Nome = "Gideon",
            Papel = "Tech lead",
            Sala = SalaEscritorio.Central,
            Ordem = 0,
            CorAvatar = "#c9a227",
            Ativo = true
        };
        db.AgentesEscritorio.Add(agente);
        await db.SaveChangesAsync();

        db.StatusAgentesAtual.Add(new StatusAgenteAtual
        {
            AgenteId = agente.Id,
            Status = StatusAgenteEscritorio.Revisando,
            TarefaAtual = "Review",
            Projeto = "Hub",
            AtualizadoEmPublicado = DateTime.UtcNow.AddHours(-2)
        });

        for (var i = 0; i < 7; i++)
        {
            db.EventosAgente.Add(new EventoAgente
            {
                AgenteId = agente.Id,
                Tipo = TipoEventoAgente.Entrega,
                Titulo = $"Entrega {i}",
                OcorridoEm = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await db.SaveChangesAsync();

        var handler = new ObterEstadoEscritorioQueryHandler(
            db,
            Options.Create(new EscritorioOptions
            {
                MinutosDesatualizado = 30,
                PollingSegundos = 20
            }));

        var result = await handler.Handle(new ObterEstadoEscritorioQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PollingSegundos.Should().Be(20);
        var dto = result.Value.Agentes.Should().ContainSingle().Subject;
        dto.Slug.Should().Be("gideon");
        dto.Status.Should().Be("revisando");
        dto.Desatualizado.Should().BeTrue();
        dto.UltimasEntregas.Should().HaveCount(5);
        dto.UltimasEntregas[0].Titulo.Should().Be("Entrega 0");
    }

    [Theory]
    [InlineData(null, "sem atualização")]
    public void FormatarHaQuantoTempo_SemData(DateTime? em, string esperado)
    {
        ObterEstadoEscritorioQueryHandler
            .FormatarHaQuantoTempo(em, DateTime.UtcNow)
            .Should().Be(esperado);
    }
}
