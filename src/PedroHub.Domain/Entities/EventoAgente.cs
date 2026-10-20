using PedroHub.Domain.Common;
using PedroHub.Domain.Enums;

namespace PedroHub.Domain.Entities;

public class EventoAgente : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgenteId { get; set; }
    public AgenteEscritorio? Agente { get; set; }
    public TipoEventoAgente Tipo { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Link { get; set; }
    public DateTime OcorridoEm { get; set; }
}