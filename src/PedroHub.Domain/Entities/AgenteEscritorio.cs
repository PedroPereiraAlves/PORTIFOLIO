using PedroHub.Domain.Common;
using PedroHub.Domain.Enums;

namespace PedroHub.Domain.Entities;

public class AgenteEscritorio : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Papel { get; set; } = string.Empty;
    public SalaEscritorio Sala { get; set; }
    public int Ordem { get; set; }
    public string CorAvatar { get; set; } = "#1e5a8a";
    public string SpriteKey { get; set; } = "agent";
    public bool Ativo { get; set; } = true;

    public StatusAgenteAtual? StatusAtual { get; set; }
    public ICollection<StatusAgenteHistorico> Historico { get; set; } = new List<StatusAgenteHistorico>();
    public ICollection<EventoAgente> Eventos { get; set; } = new List<EventoAgente>();
}
