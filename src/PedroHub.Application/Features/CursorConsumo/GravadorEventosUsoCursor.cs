using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.CursorConsumo.Dtos;
using PedroHub.Domain.CursorConsumo;
using PedroHub.Domain.Entities;

namespace PedroHub.Application.Features.CursorConsumo;

public static class GravadorEventosUsoCursor
{
    public const int TamanhoLoteChaves = 500;

    public static async Task<ResultadoImportacaoUsoDto> GravarAsync(
        IApplicationDbContext db,
        IReadOnlyList<EventoUsoNormalizado> eventos,
        string origem,
        string? nomeArquivo,
        int linhasLidas,
        int ignorados,
        string? aviso,
        CancellationToken cancellationToken)
    {
        var preparados = new List<(EventoUsoNormalizado Evento, string Chave)>(eventos.Count);
        var vistos = new HashSet<string>(StringComparer.Ordinal);
        var duplicadosNoLote = 0;

        foreach (var evento in eventos)
        {
            var chave = ChaveEventoUsoCursor.Calcular(evento);
            if (!vistos.Add(chave))
            {
                duplicadosNoLote++;
                continue;
            }

            preparados.Add((evento, chave));
        }

        var existentes = new HashSet<string>(StringComparer.Ordinal);
        var chaves = preparados.Select(p => p.Chave).ToList();
        for (var i = 0; i < chaves.Count; i += TamanhoLoteChaves)
        {
            var lote = chaves.Skip(i).Take(TamanhoLoteChaves).ToList();
            var achadas = await db.EventosUsoCursor
                .AsNoTracking()
                .Where(e => lote.Contains(e.ChaveNatural))
                .Select(e => e.ChaveNatural)
                .ToListAsync(cancellationToken);

            foreach (var achada in achadas)
                existentes.Add(achada);
        }

        var agora = DateTime.UtcNow;
        var novos = new List<EventoUsoCursor>();
        foreach (var (evento, chave) in preparados)
        {
            if (!existentes.Add(chave))
                continue;

            novos.Add(new EventoUsoCursor
            {
                OcorridoEm = DateTime.SpecifyKind(evento.OcorridoEmUtc, DateTimeKind.Utc),
                Email = evento.Email,
                Modelo = evento.Modelo,
                Tipo = evento.Tipo,
                ModoMaximo = evento.ModoMaximo,
                TokensEntrada = evento.TokensEntrada,
                TokensCacheEscrita = evento.TokensCacheEscrita,
                TokensCacheLeitura = evento.TokensCacheLeitura,
                TokensSaida = evento.TokensSaida,
                TokensTotal = evento.TokensTotal,
                CustoCentavos = evento.CustoCentavos,
                UnidadesRequisicao = evento.UnidadesRequisicao,
                Origem = origem,
                ChaveNatural = chave,
                CriadoEm = agora
            });
        }

        var avisoFinal = MontarAviso(aviso, duplicadosNoLote);
        db.EventosUsoCursor.AddRange(novos);
        db.ImportacoesUsoCursor.Add(new ImportacaoUsoCursor
        {
            Origem = origem,
            NomeArquivo = nomeArquivo,
            LinhasLidas = linhasLidas,
            Inseridos = novos.Count,
            Duplicados = duplicadosNoLote + (preparados.Count - novos.Count),
            Ignorados = ignorados,
            Aviso = avisoFinal,
            CriadoEm = agora
        });

        await db.SaveChangesAsync(cancellationToken);

        return new ResultadoImportacaoUsoDto(
            linhasLidas,
            novos.Count,
            duplicadosNoLote + (preparados.Count - novos.Count),
            ignorados,
            avisoFinal);
    }

    private static string? MontarAviso(string? aviso, int duplicadosNoLote)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(aviso))
            partes.Add(aviso.Trim());
        if (duplicadosNoLote > 0)
            partes.Add($"{duplicadosNoLote} linha(s) repetida(s) no próprio arquivo foram ignoradas.");

        if (partes.Count == 0)
            return null;

        var texto = string.Join(" ", partes);
        return texto.Length <= 500 ? texto : texto[..500];
    }
}
