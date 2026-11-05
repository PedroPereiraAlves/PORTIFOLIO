using System.Globalization;
using System.Text;

namespace PedroHub.Domain.CursorConsumo;

public static class LeitorCsvUsoCursor
{
    public const int MaxLinhas = 100_000;

    public static ResultadoLeituraCsv Ler(string? conteudo)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
            return ResultadoLeituraCsv.Falha("O arquivo CSV está vazio.");

        if (conteudo.Contains('\0'))
            return ResultadoLeituraCsv.Falha("O arquivo não parece um CSV de texto.");

        var registros = SepararRegistros(conteudo);
        if (registros.Count == 0 || registros.All(string.IsNullOrWhiteSpace))
            return ResultadoLeituraCsv.Falha("O arquivo CSV está vazio.");

        var indiceCabecalho = registros.FindIndex(r => !string.IsNullOrWhiteSpace(r));
        var mapa = Mapear(SepararCampos(registros[indiceCabecalho]));
        if (mapa.Data < 0)
            return ResultadoLeituraCsv.Falha("O CSV precisa da coluna Date (ou Data).");

        if (mapa.Modelo < 0 && mapa.Total < 0 && mapa.Entrada < 0 && mapa.Saida < 0 && mapa.CacheEscrita < 0)
            return ResultadoLeituraCsv.Falha("O CSV precisa da coluna Model ou de colunas de tokens.");

        var eventos = new List<EventoUsoNormalizado>();
        var avisos = new List<string>();
        var linhasDados = 0;
        var ignorados = 0;

        for (var i = indiceCabecalho + 1; i < registros.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(registros[i]))
                continue;

            linhasDados++;
            if (eventos.Count >= MaxLinhas)
            {
                avisos.Add($"Leitura interrompida após {MaxLinhas.ToString("N0", CultureInfo.InvariantCulture)} eventos.");
                ignorados += registros.Count - i;
                break;
            }

            if (!TentarLinha(SepararCampos(registros[i]), mapa, out var evento, out var erro))
            {
                ignorados++;
                if (avisos.Count < 8 && erro is not null)
                    avisos.Add($"Linha {i + 1}: {erro}");
                continue;
            }

            eventos.Add(evento!);
        }

        return new ResultadoLeituraCsv(true, null, eventos, avisos, linhasDados, ignorados);
    }

    private static bool TentarLinha(
        IReadOnlyList<string> campos,
        MapaColunas mapa,
        out EventoUsoNormalizado? evento,
        out string? erro)
    {
        evento = null;
        erro = null;

        if (!TentarData(Campo(campos, mapa.Data), out var ocorrido))
        {
            erro = "data inválida";
            return false;
        }

        if (!TentarLong(Campo(campos, mapa.Entrada), out var entrada)
            || !TentarLong(Campo(campos, mapa.CacheEscrita), out var cacheEscrita)
            || !TentarLong(Campo(campos, mapa.CacheLeitura), out var cacheLeitura)
            || !TentarLong(Campo(campos, mapa.Saida), out var saida)
            || !TentarLong(Campo(campos, mapa.Total), out var total))
        {
            erro = "quantidade de tokens inválida";
            return false;
        }

        var soma = entrada + cacheEscrita + cacheLeitura + saida;
        if (total <= 0 && soma > 0)
            total = soma;

        if (!TentarCusto(Campo(campos, mapa.Custo), mapa.CustoEmCentavos, out var centavos, out erro))
            return false;

        decimal? unidades = null;
        if (mapa.Unidades >= 0)
        {
            if (!TentarDecimal(Campo(campos, mapa.Unidades), out var lidas))
            {
                erro = "unidades de requisição inválidas";
                return false;
            }

            unidades = lidas;
        }

        var modelo = Truncar(Campo(campos, mapa.Modelo), 160);
        if (string.IsNullOrWhiteSpace(modelo))
            modelo = "desconhecido";

        var email = Truncar(Campo(campos, mapa.Email), 320);
        var tipo = Truncar(Campo(campos, mapa.Tipo), 80);

        evento = new EventoUsoNormalizado(
            ocorrido,
            string.IsNullOrWhiteSpace(email) ? null : email,
            modelo,
            string.IsNullOrWhiteSpace(tipo) ? null : tipo,
            EhSim(Campo(campos, mapa.ModoMaximo)),
            entrada,
            cacheEscrita,
            cacheLeitura,
            saida,
            total,
            centavos,
            unidades);
        return true;
    }

    private readonly record struct MapaColunas(
        int Data,
        int Email,
        int Modelo,
        int Tipo,
        int ModoMaximo,
        int Entrada,
        int CacheEscrita,
        int CacheLeitura,
        int Saida,
        int Total,
        int Custo,
        bool CustoEmCentavos,
        int Unidades);

    private static MapaColunas Mapear(IReadOnlyList<string> cabecalho)
    {
        int data = -1, email = -1, modelo = -1, tipo = -1, modo = -1;
        int entrada = -1, cacheEscrita = -1, cacheLeitura = -1, saida = -1, total = -1;
        int custo = -1, unidades = -1;
        var custoEmCentavos = false;

        for (var i = 0; i < cabecalho.Count; i++)
        {
            var nome = Normalizar(cabecalho[i]);
            if (nome.Length == 0)
                continue;

            if (data < 0 && nome is "date" or "data" or "timestamp" or "occurred at")
                data = i;
            else if (email < 0 && nome is "user" or "usuario" or "usuário" or "email" or "user email" or "useremail")
                email = i;
            else if (modelo < 0 && nome is "model" or "modelo")
                modelo = i;
            else if (tipo < 0 && nome is "kind" or "tipo" or "billing" or "type")
                tipo = i;
            else if (modo < 0 && nome is "max mode" or "maxmode" or "modo max")
                modo = i;
            else if (cacheEscrita < 0 && nome is "input (w/ cache write)" or "input (w/cache write)" or "cache write" or "cache write tokens" or "cachewritetokens")
                cacheEscrita = i;
            else if (entrada < 0 && nome is "input (w/o cache write)" or "input (w/o cachewrite)" or "input tokens" or "input" or "inputtokens")
                entrada = i;
            else if (cacheLeitura < 0 && nome is "cache read" or "cache read tokens" or "cachereadtokens")
                cacheLeitura = i;
            else if (saida < 0 && nome is "output tokens" or "output" or "outputtokens")
                saida = i;
            else if (total < 0 && nome is "total tokens" or "total" or "totaltokens")
                total = i;
            else if (custo < 0 && nome is "cost" or "custo" or "charged cents" or "chargedcents" or "custo centavos")
            {
                custo = i;
                custoEmCentavos = nome is "charged cents" or "chargedcents" or "custo centavos";
            }
            else if (unidades < 0 && nome is "requests" or "request costs" or "requestscosts" or "requisicoes" or "requisições")
                unidades = i;
        }

        return new MapaColunas(
            data, email, modelo, tipo, modo, entrada, cacheEscrita, cacheLeitura, saida, total, custo, custoEmCentavos, unidades);
    }

    private static string Normalizar(string nome)
    {
        var s = nome.Trim().TrimStart('\uFEFF').ToLowerInvariant().Replace('\u00A0', ' ');
        while (s.Contains("  ", StringComparison.Ordinal))
            s = s.Replace("  ", " ", StringComparison.Ordinal);
        return s;
    }

    private static string? Campo(IReadOnlyList<string> campos, int indice) =>
        indice >= 0 && indice < campos.Count ? campos[indice] : null;

    private static string? Truncar(string? valor, int max)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;
        var s = valor.Trim();
        return s.Length <= max ? s : s[..max];
    }

    private static bool EhSim(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        return valor.Trim().ToLowerInvariant() is "yes" or "y" or "true" or "1" or "sim" or "s";
    }

    private static bool TentarData(string? bruto, out DateTime utc)
    {
        utc = default;
        if (string.IsNullOrWhiteSpace(bruto))
            return false;

        var s = bruto.Trim();
        if (s.Length >= 12
            && long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epochMs))
        {
            try
            {
                utc = DateTimeOffset.FromUnixTimeMilliseconds(epochMs).UtcDateTime;
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        if (DateTimeOffset.TryParse(
                s,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var instante))
        {
            utc = instante.UtcDateTime;
            return true;
        }

        return false;
    }

    private static bool TentarLong(string? bruto, out long valor)
    {
        valor = 0;
        if (string.IsNullOrWhiteSpace(bruto) || bruto.Trim() is "-" or "—" or "–")
            return true;

        if (!TentarDecimal(bruto, out var numero) || numero is null)
            return false;

        valor = (long)decimal.Round(numero.Value, 0, MidpointRounding.AwayFromZero);
        return true;
    }

    private static bool TentarDecimal(string? bruto, out decimal? valor)
    {
        valor = null;
        if (string.IsNullOrWhiteSpace(bruto) || bruto.Trim() is "-" or "—" or "–")
            return true;

        var s = bruto.Trim().Replace(" ", "", StringComparison.Ordinal);
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariante))
        {
            valor = invariante;
            return true;
        }

        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out var pt))
        {
            valor = pt;
            return true;
        }

        return false;
    }

    private static bool TentarCusto(string? bruto, bool emCentavos, out decimal? centavos, out string? erro)
    {
        centavos = null;
        erro = null;
        if (string.IsNullOrWhiteSpace(bruto))
            return true;

        var s = bruto.Trim();
        if (s is "-" or "—" or "–")
            return true;

        var lower = s.ToLowerInvariant();
        if (lower is "free" or "gratis" or "grátis" or "$0" or "$0.00" or "us$ 0,00" or "us$0")
        {
            centavos = 0;
            return true;
        }

        if (lower is "included" or "incluído" or "incluido" or "included in business")
            return true;

        var comoDolar = false;
        if (s.StartsWith("US$", StringComparison.OrdinalIgnoreCase))
        {
            s = s[3..].Trim();
            comoDolar = true;
        }
        else if (s.StartsWith("USD", StringComparison.OrdinalIgnoreCase))
        {
            s = s[3..].Trim();
            comoDolar = true;
        }
        else if (s.StartsWith('$'))
        {
            s = s[1..].Trim();
            comoDolar = true;
        }

        if (!TentarDecimal(s, out var numero) || numero is null)
        {
            erro = "custo inválido";
            return false;
        }

        centavos = emCentavos && !comoDolar
            ? decimal.Round(numero.Value, 6, MidpointRounding.AwayFromZero)
            : decimal.Round(numero.Value * 100m, 6, MidpointRounding.AwayFromZero);
        return true;
    }

    internal static List<string> SepararRegistros(string texto)
    {
        var registros = new List<string>();
        var atual = new StringBuilder();
        var aspas = false;

        for (var i = 0; i < texto.Length; i++)
        {
            var c = texto[i];
            if (c == '"')
            {
                atual.Append(c);
                if (aspas && i + 1 < texto.Length && texto[i + 1] == '"')
                {
                    atual.Append('"');
                    i++;
                    continue;
                }

                aspas = !aspas;
                continue;
            }

            if ((c == '\n' || c == '\r') && !aspas)
            {
                if (c == '\r' && i + 1 < texto.Length && texto[i + 1] == '\n')
                    i++;
                registros.Add(atual.ToString());
                atual.Clear();
                continue;
            }

            atual.Append(c);
        }

        if (atual.Length > 0)
            registros.Add(atual.ToString());

        return registros;
    }

    internal static List<string> SepararCampos(string registro)
    {
        var campos = new List<string>();
        var atual = new StringBuilder();
        var aspas = false;

        for (var i = 0; i < registro.Length; i++)
        {
            var c = registro[i];
            if (c == '"')
            {
                if (aspas && i + 1 < registro.Length && registro[i + 1] == '"')
                {
                    atual.Append('"');
                    i++;
                    continue;
                }

                aspas = !aspas;
                continue;
            }

            if (c == ',' && !aspas)
            {
                campos.Add(atual.ToString());
                atual.Clear();
                continue;
            }

            atual.Append(c);
        }

        campos.Add(atual.ToString());
        return campos;
    }
}
