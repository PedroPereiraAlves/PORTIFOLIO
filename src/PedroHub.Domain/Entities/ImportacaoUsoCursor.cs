using PedroHub.Domain.Common;

namespace PedroHub.Domain.Entities;

public class ImportacaoUsoCursor : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Origem { get; set; } = "csv";
    public string? NomeArquivo { get; set; }
    public int LinhasLidas { get; set; }
    public int Inseridos { get; set; }
    public int Duplicados { get; set; }
    public int Ignorados { get; set; }
    public string? Aviso { get; set; }
}
