using Microsoft.EntityFrameworkCore;
using PedroHub.Domain.Entities;

namespace PedroHub.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Projeto> Projetos { get; }
    DbSet<AlvoMonitorado> AlvosMonitorados { get; }
    DbSet<Checagem> Checagens { get; }
    DbSet<AgenteEscritorio> AgentesEscritorio { get; }
    DbSet<StatusAgenteAtual> StatusAgentesAtual { get; }
    DbSet<StatusAgenteHistorico> StatusAgentesHistorico { get; }
    DbSet<EventoAgente> EventosAgente { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
