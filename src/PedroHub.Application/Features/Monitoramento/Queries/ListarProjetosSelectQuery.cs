using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.Monitoramento.Dtos;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Monitoramento.Queries;

public sealed record ListarProjetosSelectQuery : IRequest<Result<IReadOnlyList<ProjetoSelectDto>>>;

public sealed class ListarProjetosSelectQueryHandler
    : IRequestHandler<ListarProjetosSelectQuery, Result<IReadOnlyList<ProjetoSelectDto>>>
{
    private readonly IApplicationDbContext _db;

    public ListarProjetosSelectQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ProjetoSelectDto>>> Handle(
        ListarProjetosSelectQuery request,
        CancellationToken cancellationToken)
    {
        var itens = await _db.Projetos
            .AsNoTracking()
            .OrderBy(p => p.Nome)
            .Select(p => new ProjetoSelectDto(p.Id, p.Nome))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ProjetoSelectDto>>(itens);
    }
}
