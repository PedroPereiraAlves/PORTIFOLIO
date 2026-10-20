using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.Monitoramento.Dtos;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Monitoramento.Queries;

public sealed record ListarChecagensQuery(Guid AlvoId, int Pagina = 1, int TamanhoPagina = 20)
    : IRequest<Result<ListaChecagensDto>>;

public sealed record ListaChecagensDto(
    IReadOnlyList<ChecagemDto> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total);

public sealed class ListarChecagensQueryHandler
    : IRequestHandler<ListarChecagensQuery, Result<ListaChecagensDto>>
{
    private readonly IApplicationDbContext _db;

    public ListarChecagensQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ListaChecagensDto>> Handle(
        ListarChecagensQuery request,
        CancellationToken cancellationToken)
    {
        var existe = await _db.AlvosMonitorados.AnyAsync(a => a.Id == request.AlvoId, cancellationToken);
        if (!existe)
            return Result.Failure<ListaChecagensDto>("Alvo não encontrado.");

        var pagina = Math.Max(1, request.Pagina);
        var tamanho = Math.Clamp(request.TamanhoPagina, 5, 100);

        var query = _db.Checagens.AsNoTracking()
            .Where(c => c.AlvoMonitoradoId == request.AlvoId)
            .OrderByDescending(c => c.ExecutadaEm);

        var total = await query.CountAsync(cancellationToken);
        var itens = await query
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(c => new ChecagemDto(c.Id, c.ExecutadaEm, c.Status, c.LatenciaMs, c.CodigoHttp, c.Erro))
            .ToListAsync(cancellationToken);

        return Result.Success(new ListaChecagensDto(itens, pagina, tamanho, total));
    }
}
