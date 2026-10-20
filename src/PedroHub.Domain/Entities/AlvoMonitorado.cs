using PedroHub.Domain.Common;
using PedroHub.Domain.Enums;

namespace PedroHub.Domain.Entities;

public class AlvoMonitorado : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public MetodoChecagem Metodo { get; set; } = MetodoChecagem.Get;
    public int IntervaloSegundos { get; set; } = 300;
    public int TimeoutMs { get; set; } = 10_000;
    /// <summary>Código único ("200") ou faixa ("200-399").</summary>
    public string StatusEsperado { get; set; } = "200";
    public int LimiarDegradadoMs { get; set; } = 1500;
    public bool Ativo { get; set; } = true;
    public Guid? ProjetoId { get; set; }
    public Projeto? Projeto { get; set; }
    public string? Infra { get; set; }

    public StatusAlvo StatusAtual { get; set; } = StatusAlvo.Desconhecido;
    public int FalhasConsecutivas { get; set; }
    public DateTime? UltimaChecagemEm { get; set; }
    public DateTime? ProximaChecagemEm { get; set; }
    public DateTime? UltimaMudancaStatusEm { get; set; }

    public ICollection<Checagem> Checagens { get; set; } = new List<Checagem>();
}
