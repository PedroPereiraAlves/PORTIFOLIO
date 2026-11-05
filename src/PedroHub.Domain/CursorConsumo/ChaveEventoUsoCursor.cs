using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PedroHub.Domain.CursorConsumo;

public static class ChaveEventoUsoCursor
{
    public static string Calcular(EventoUsoNormalizado evento)
    {
        var ocorrido = DateTime.SpecifyKind(evento.OcorridoEmUtc, DateTimeKind.Utc);
        var bruto = string.Join(
            '|',
            ocorrido.ToString("O", CultureInfo.InvariantCulture),
            evento.Email ?? "",
            evento.Modelo,
            evento.Tipo ?? "",
            evento.ModoMaximo ? "1" : "0",
            evento.TokensEntrada.ToString(CultureInfo.InvariantCulture),
            evento.TokensCacheEscrita.ToString(CultureInfo.InvariantCulture),
            evento.TokensCacheLeitura.ToString(CultureInfo.InvariantCulture),
            evento.TokensSaida.ToString(CultureInfo.InvariantCulture),
            evento.TokensTotal.ToString(CultureInfo.InvariantCulture),
            evento.CustoCentavos?.ToString(CultureInfo.InvariantCulture) ?? "",
            evento.UnidadesRequisicao?.ToString(CultureInfo.InvariantCulture) ?? "");

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(bruto));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
