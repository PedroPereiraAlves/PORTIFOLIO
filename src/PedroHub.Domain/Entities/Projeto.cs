using PedroHub.Domain.Common;
using PedroHub.Domain.Enums;

namespace PedroHub.Domain.Entities;

public class Projeto : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string UrlRepositorio { get; set; } = string.Empty;
    public string? UrlDemo { get; set; }
    public string? Linguagem { get; set; }
    public int Stars { get; set; }
    public string? Topicos { get; set; }
    public bool Visivel { get; set; } = true;
    public bool Destaque { get; set; }
    public int Ordem { get; set; }
    public OrigemProjeto Origem { get; set; } = OrigemProjeto.Manual;
    public DateTime? SincronizadoEm { get; set; }
}
