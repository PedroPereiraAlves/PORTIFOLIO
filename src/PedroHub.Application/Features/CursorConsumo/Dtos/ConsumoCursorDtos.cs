namespace PedroHub.Application.Features.CursorConsumo.Dtos;

public sealed record ConsumoDiaDto(DateOnly Dia, int Requisicoes, long Tokens, decimal? CustoCentavos);

public sealed record ConsumoModeloDto(
    string Modelo,
    int Requisicoes,
    long Tokens,
    decimal? CustoCentavos,
    decimal PercentualTokens);

public sealed record ConsumoCursorDto(
    string Periodo,
    DateTime PeriodoInicioUtc,
    DateTime PeriodoFimUtc,
    DateTime CicloInicioUtc,
    DateTime CicloFimUtc,
    int Requisicoes,
    long Tokens,
    decimal? CustoCentavos,
    bool CustoIncompleto,
    int RequisicoesCiclo,
    long TokensCiclo,
    decimal? CustoCentavosCiclo,
    bool CustoCicloIncompleto,
    decimal? PercentualCotaTokens,
    decimal? PercentualCotaUsd,
    long? CotaTokens,
    decimal? CotaUsd,
    bool ApiConfigurada,
    DateTime? UltimaImportacaoEm,
    string? UltimaOrigem,
    IReadOnlyList<ConsumoDiaDto> PorDia,
    IReadOnlyList<ConsumoModeloDto> PorModelo);

public sealed record ResultadoImportacaoUsoDto(
    int LinhasLidas,
    int Inseridos,
    int Duplicados,
    int Ignorados,
    string? Aviso);
