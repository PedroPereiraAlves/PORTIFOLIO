namespace PedroHub.Domain.Monitoramento;

public static class StatusEsperadoParser
{
    public static bool TentarParse(string? raw, out int min, out int max)
    {
        min = 0;
        max = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var texto = raw.Trim();
        if (texto.Contains('-', StringComparison.Ordinal))
        {
            var partes = texto.Split('-', 2, StringSplitOptions.TrimEntries);
            if (partes.Length != 2
                || !int.TryParse(partes[0], out min)
                || !int.TryParse(partes[1], out max)
                || min < 100 || max > 599 || min > max)
                return false;
            return true;
        }

        if (!int.TryParse(texto, out min) || min < 100 || min > 599)
            return false;

        max = min;
        return true;
    }

    public static bool Corresponde(string statusEsperado, int? codigoHttp)
    {
        if (codigoHttp is null)
            return false;

        if (!TentarParse(statusEsperado, out var min, out var max))
            return false;

        return codigoHttp.Value >= min && codigoHttp.Value <= max;
    }
}
