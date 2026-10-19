namespace PedroHub.Domain.Common;

public abstract class AuditableEntity
{
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}
