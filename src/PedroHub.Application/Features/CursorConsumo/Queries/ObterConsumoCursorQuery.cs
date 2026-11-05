using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Options;
using PedroHub.Application.Features.CursorConsumo.Dtos;
using PedroHub.Domain.CursorConsumo;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.CursorConsumo.Queries;

public sealed record ObterConsumoCursorQuery(string? Periodo)
    : IRequest<Result<ConsumoCursorDto>>;

public sealed class ObterConsumoCursorQueryHandler
    : IRequestHandler<ObterConsumoCursorQuery, Result<ConsumoCursorDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly CursorConsumoOptions _options;

    public ObterConsumoCursorQueryHandler(IApplicationDbContext db, IOptions<CursorConsumoOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<Result<ConsumoCursorDto>> Handle(
        ObterConsumoCursorQuery request,
        CancellationToken cancellationToken)
    {
        var agora = DateTime.UtcNow;
        var diaCiclo = _options.DiaInicioCiclo;
        var periodo = JanelaConsumoCursor.NormalizarPeriodo(request.Periodo);
        var janela = JanelaConsumoCursor.Resolver(periodo, agora, diaCiclo);
        var ciclo = JanelaConsumoCursor.Ciclo(agora, diaCiclo);
        var desde = janela.InicioUtc < ciclo.InicioUtc ? janela.InicioUtc : ciclo.InicioUtc;
        var ate = janela.FimExclusivoUtc > ciclo.FimExclusivoUtc ? janela.FimExclusivoUtc : ciclo.FimExclusivoUtc;

        var linhas = await _db.EventosUsoCursor
            .AsNoTracking()
            .Where(e => e.OcorridoEm >= desde && e.OcorridoEm < ate)
            .Select(e => new
            {
                e.OcorridoEm,
                e.Email,
                e.Modelo,
                e.Tipo,
                e.ModoMaximo,
                e.TokensEntrada,
                e.TokensCacheEscrita,
                e.TokensCacheLeitura,
                e.TokensSaida,
                e.TokensTotal,
                e.CustoCentavos,
                e.UnidadesRequisicao
            })
            .ToListAsync(cancellationToken);

        var eventos = linhas
            .Select(e => new EventoUsoNormalizado(
                e.OcorridoEm,
                e.Email,
                e.Modelo,
                e.Tipo,
                e.ModoMaximo,
                e.TokensEntrada,
                e.TokensCacheEscrita,
                e.TokensCacheLeitura,
                e.TokensSaida,
                e.TokensTotal,
                e.CustoCentavos,
                e.UnidadesRequisicao))
            .ToList();

        var agregado = AgregadorConsumoCursor.Agregar(
            eventos,
            janela.InicioUtc,
            janela.FimExclusivoUtc,
            ciclo.InicioUtc,
            ciclo.FimExclusivoUtc,
            _options.CotaTokens is > 0 ? _options.CotaTokens : null,
            _options.CotaUsd is > 0 ? _options.CotaUsd : null);

        var ultima = await _db.ImportacoesUsoCursor
            .AsNoTracking()
            .OrderByDescending(i => i.CriadoEm)
            .Select(i => new { i.CriadoEm, i.Origem })
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success(new ConsumoCursorDto(
            periodo,
            janela.InicioUtc,
            janela.FimExclusivoUtc,
            ciclo.InicioUtc,
            ciclo.FimExclusivoUtc,
            agregado.Requisicoes,
            agregado.Tokens,
            agregado.CustoCentavos,
            agregado.CustoIncompleto,
            agregado.RequisicoesCiclo,
            agregado.TokensCiclo,
            agregado.CustoCentavosCiclo,
            agregado.CustoCicloIncompleto,
            agregado.PercentualCotaTokens,
            agregado.PercentualCotaUsd,
            _options.CotaTokens is > 0 ? _options.CotaTokens : null,
            _options.CotaUsd is > 0 ? _options.CotaUsd : null,
            !string.IsNullOrWhiteSpace(_options.ApiKey),
            ultima?.CriadoEm,
            ultima?.Origem,
            agregado.PorDia.Select(d => new ConsumoDiaDto(d.Dia, d.Requisicoes, d.Tokens, d.CustoCentavos)).ToList(),
            agregado.PorModelo.Select(m => new ConsumoModeloDto(m.Modelo, m.Requisicoes, m.Tokens, m.CustoCentavos, m.PercentualTokens)).ToList()));
    }
}
