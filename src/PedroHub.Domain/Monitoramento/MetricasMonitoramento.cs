using PedroHub.Domain.Enums;

namespace PedroHub.Domain.Monitoramento;

public static class MetricasMonitoramento
{
    /// <summary>
    /// Uptime: checagens Online ou Degradado ÷ total.
    /// Degradado conta como "up" (serviço respondeu no status esperado, só estava lento).
    /// </summary>
    public static double CalcularUptime(IReadOnlyList<StatusAlvo> statuses)
    {
        if (statuses.Count == 0)
            return 0;

        var up = statuses.Count(s => s is StatusAlvo.Online or StatusAlvo.Degradado);
        return (double)up / statuses.Count;
    }

    public static int Percentil(IReadOnlyList<int> valoresOrdenados, double p)
    {
        if (valoresOrdenados.Count == 0)
            return 0;

        if (valoresOrdenados.Count == 1)
            return valoresOrdenados[0];

        var rank = p * (valoresOrdenados.Count - 1);
        var low = (int)Math.Floor(rank);
        var high = (int)Math.Ceiling(rank);
        if (low == high)
            return valoresOrdenados[low];

        var weight = rank - low;
        return (int)Math.Round(
            valoresOrdenados[low] * (1 - weight) + valoresOrdenados[high] * weight,
            MidpointRounding.AwayFromZero);
    }

    public static (int P50, int P95) CalcularPercentisLatencia(IEnumerable<int> latencias)
    {
        var ordenadas = latencias.OrderBy(x => x).ToList();
        return (Percentil(ordenadas, 0.50), Percentil(ordenadas, 0.95));
    }
}
