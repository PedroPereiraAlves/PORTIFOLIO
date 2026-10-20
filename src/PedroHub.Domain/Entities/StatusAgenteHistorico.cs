using PedroHub.Domain.Common;
using PedroHub.Domain.Enums;

namespace PedroHub.Domain.Entities;

public class StatusAgenteHistorico : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgenteId { get; set; }
    public AgenteEscritorio? Agente { get; set; }
    public StatusAgenteEscritorio Status { get; set; }
    public string? TarefaAtual { get; set; }
    public string? Projeto { get; set; }
    public string? PrUrl { get; set; }
    /// <summary>Momento publicado pelo Gideon (não confundir com AuditableEntity.AtualizadoEm).</summary>
    public DateTime PublicadoEm { get; set; }
}
