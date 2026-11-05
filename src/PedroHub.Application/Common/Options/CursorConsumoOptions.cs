namespace PedroHub.Application.Common.Options;

public sealed class CursorConsumoOptions
{
    public const string SectionName = "CursorConsumo";

    public string? ApiKey { get; set; }

    /// <summary>Host oficial. O cliente recusa outro host.</summary>
    public string BaseUrl { get; set; } = "https://api.cursor.com";

    public long? CotaTokens { get; set; }

    /// <summary>Orçamento do ciclo em dólares, não em centavos.</summary>
    public decimal? CotaUsd { get; set; }

    public int DiaInicioCiclo { get; set; } = 1;
}
