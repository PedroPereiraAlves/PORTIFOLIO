namespace PedroHub.Domain.CursorConsumo;

/// <summary>Evento de uso já normalizado, vindo do CSV do dashboard ou da Admin API.</summary>
public sealed record EventoUsoNormalizado(
    DateTime OcorridoEmUtc,
    string? Email,
    string Modelo,
    string? Tipo,
    bool ModoMaximo,
    long TokensEntrada,
    long TokensCacheEscrita,
    long TokensCacheLeitura,
    long TokensSaida,
    long TokensTotal,
    decimal? CustoCentavos,
    decimal? UnidadesRequisicao);
