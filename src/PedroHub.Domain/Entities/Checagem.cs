using PedroHub.Domain.Common;
using PedroHub.Domain.Enums;

namespace PedroHub.Domain.Entities;

public class Checagem : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AlvoMonitoradoId { get; set; }
    public AlvoMonitorado? AlvoMonitorado { get; set; }
    public DateTime ExecutadaEm { get; set; }
    public StatusAlvo Status { get; set; }
    public int LatenciaMs { get; set; }
    public int? CodigoHttp { get; set; }
    public string? Erro { get; set; }
}
