using PedroHub.Domain.Common;

namespace PedroHub.Domain.Entities;

public class EventoUsoCursor : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OcorridoEm { get; set; }
    public string? Email { get; set; }
    public string Modelo { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public bool ModoMaximo { get; set; }
    public long TokensEntrada { get; set; }
    public long TokensCacheEscrita { get; set; }
    public long TokensCacheLeitura { get; set; }
    public long TokensSaida { get; set; }
    public long TokensTotal { get; set; }
    public decimal? CustoCentavos { get; set; }
    public decimal? UnidadesRequisicao { get; set; }
    public string Origem { get; set; } = "csv";
    public string ChaveNatural { get; set; } = string.Empty;
}
