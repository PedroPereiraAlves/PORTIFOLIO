using PedroHub.Domain.Common;
using PedroHub.Domain.Enums;

namespace PedroHub.Domain.Entities;

public class StatusAgenteAtual : AuditableEntity
{
    public Guid AgenteId { get; set; }
    public AgenteEscritorio? Agente { get; set; }
    public StatusAgenteEscritorio Status { get; set; } = StatusAgenteEscritorio.Ocioso;
    public string? TarefaAtual { get; set; }
    public string? Projeto { get; set; }
    public string? PrUrl { get; set; }
    public DateTime AtualizadoEmPublicado { get; set; }
}
