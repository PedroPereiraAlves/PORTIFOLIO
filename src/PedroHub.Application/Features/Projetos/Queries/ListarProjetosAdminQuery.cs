using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.Projetos.Dtos;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Projetos.Queries;

public sealed record ListarProjetosAdminQuery : IRequest<Result<IReadOnlyList<ProjetoDto>>>;

public sealed class ListarProjetosAdminQueryHandler
    : IRequestHandler<ListarProjetosAdminQuery, Result<IReadOnlyList<ProjetoDto>>>
{
    private readonly IApplicationDbContext _db;

    public ListarProjetosAdminQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ProjetoDto>>> Handle(
        ListarProjetosAdminQuery request,
        CancellationToken cancellationToken)
    {
        var projetos = await _db.Projetos
            .AsNoTracking()
            .OrderBy(p => p.Ordem)
            .ThenByDescending(p => p.Destaque)
            .ThenBy(p => p.Nome)
            .Select(p => new ProjetoDto(
                p.Id,
                p.Nome,
                p.Descricao,
                p.UrlRepositorio,
                p.UrlDemo,
                p.Linguagem,
                p.Stars,
                p.Topicos,
                p.Visivel,
                p.Destaque,
                p.Ordem,
                p.Origem,
                p.SincronizadoEm))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ProjetoDto>>(projetos);
    }
}
