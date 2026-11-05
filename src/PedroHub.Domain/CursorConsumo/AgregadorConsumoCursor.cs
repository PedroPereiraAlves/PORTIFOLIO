namespace PedroHub.Domain.CursorConsumo;

public sealed record ConsumoDia(
    DateOnly Dia,
    int Requisicoes,
    long Tokens,
    decimal? CustoCentavos);

public sealed record ConsumoModelo(
    string Modelo,
    int Requisicoes,
    long Tokens,
    decimal? CustoCentavos,
    decimal PercentualTokens);

public sealed record ConsumoAgregado(
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
    IReadOnlyList<ConsumoDia> PorDia,
    IReadOnlyList<ConsumoModelo> PorModelo);

public static class AgregadorConsumoCursor
{
    public static ConsumoAgregado Agregar(
        IReadOnlyList<EventoUsoNormalizado> eventos,
        DateTime periodoInicioUtc,
        DateTime periodoFimExclusivoUtc,
        DateTime cicloInicioUtc,
        DateTime cicloFimExclusivoUtc,
        long? cotaTokens,
        decimal? cotaUsd)
    {
        var doPeriodo = Filtrar(eventos, periodoInicioUtc, periodoFimExclusivoUtc);
        var doCiclo = Filtrar(eventos, cicloInicioUtc, cicloFimExclusivoUtc);

        var (custo, incompleto) = SomarCusto(doPeriodo);
        var (custoCiclo, incompletoCiclo) = SomarCusto(doCiclo);
        var tokensCiclo = doCiclo.Sum(e => e.TokensTotal);

        return new ConsumoAgregado(
            doPeriodo.Count,
            doPeriodo.Sum(e => e.TokensTotal),
            custo,
            incompleto,
            doCiclo.Count,
            tokensCiclo,
            custoCiclo,
            incompletoCiclo,
            PercentualTokens(tokensCiclo, cotaTokens),
            PercentualUsd(custoCiclo, cotaUsd),
            PorDia(doPeriodo, periodoInicioUtc, periodoFimExclusivoUtc),
            PorModelo(doPeriodo));
    }

    public static decimal? PercentualTokens(long tokens, long? cota)
    {
        if (cota is not > 0)
            return null;

        return decimal.Round(tokens * 100m / cota.Value, 1, MidpointRounding.AwayFromZero);
    }

    public static decimal? PercentualUsd(decimal? custoCentavos, decimal? cotaUsd)
    {
        if (cotaUsd is not > 0 || custoCentavos is null)
            return null;

        return decimal.Round(custoCentavos.Value / cotaUsd.Value, 1, MidpointRounding.AwayFromZero);
    }

    private static List<EventoUsoNormalizado> Filtrar(
        IReadOnlyList<EventoUsoNormalizado> eventos,
        DateTime inicioUtc,
        DateTime fimExclusivoUtc)
    {
        var inicio = EmUtc(inicioUtc);
        var fim = EmUtc(fimExclusivoUtc);
        return eventos
            .Where(e =>
            {
                var quando = EmUtc(e.OcorridoEmUtc);
                return quando >= inicio && quando < fim;
            })
            .ToList();
    }

    private static (decimal? Custo, bool Incompleto) SomarCusto(IReadOnlyList<EventoUsoNormalizado> eventos)
    {
        if (eventos.Count == 0)
            return (0, false);

        var conhecidos = eventos.Where(e => e.CustoCentavos is not null).ToList();
        if (conhecidos.Count == 0)
            return (null, true);

        return (conhecidos.Sum(e => e.CustoCentavos!.Value), conhecidos.Count < eventos.Count);
    }

    private static List<ConsumoDia> PorDia(
        IReadOnlyList<EventoUsoNormalizado> eventos,
        DateTime inicioUtc,
        DateTime fimExclusivoUtc)
    {
        var inicio = DateOnly.FromDateTime(EmUtc(inicioUtc));
        var fim = DateOnly.FromDateTime(EmUtc(fimExclusivoUtc).AddTicks(-1));
        if (fim < inicio)
            fim = inicio;

        var porDia = eventos
            .GroupBy(e => DateOnly.FromDateTime(EmUtc(e.OcorridoEmUtc)))
            .ToDictionary(g => g.Key, g => g.ToList());

        var serie = new List<ConsumoDia>();
        for (var dia = inicio; dia <= fim; dia = dia.AddDays(1))
        {
            if (!porDia.TryGetValue(dia, out var itens))
            {
                serie.Add(new ConsumoDia(dia, 0, 0, 0));
                continue;
            }

            var (custo, _) = SomarCusto(itens);
            serie.Add(new ConsumoDia(dia, itens.Count, itens.Sum(e => e.TokensTotal), custo ?? 0));
        }

        return serie;
    }

    private static List<ConsumoModelo> PorModelo(IReadOnlyList<EventoUsoNormalizado> eventos)
    {
        var total = eventos.Sum(e => e.TokensTotal);
        return eventos
            .GroupBy(e => e.Modelo, StringComparer.Ordinal)
            .Select(g =>
            {
                var tokens = g.Sum(e => e.TokensTotal);
                var (custo, _) = SomarCusto(g.ToList());
                var percentual = total <= 0
                    ? 0
                    : decimal.Round(tokens * 100m / total, 1, MidpointRounding.AwayFromZero);
                return new ConsumoModelo(g.Key, g.Count(), tokens, custo, percentual);
            })
            .OrderByDescending(m => m.Tokens)
            .ThenBy(m => m.Modelo, StringComparer.Ordinal)
            .ToList();
    }

    private static DateTime EmUtc(DateTime valor) =>
        valor.Kind == DateTimeKind.Utc ? valor : DateTime.SpecifyKind(valor, DateTimeKind.Utc);
}
