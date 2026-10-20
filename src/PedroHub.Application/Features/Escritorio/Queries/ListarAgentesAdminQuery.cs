using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.Escritorio.Dtos;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Escritorio.Queries;

public sealed record ListarAgentesAdminQuery : IRequest<Result<IReadOnlyList<AgenteAdminDto>>>;

public sealed class ListarAgentesAdminQueryHandler
    : IRequestHandler<ListarAgentesAdminQuery, Result<IReadOnlyList<AgenteAdminDto>>>
{
    private readonly IApplicationDbContext _db;

    public ListarAgentesAdminQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<AgenteAdminDto>>> Handle(
        ListarAgentesAdminQuery request,
        CancellationToken cancellationToken)
    {
        var lista = await _db.AgentesEscritorio
            .AsNoTracking()
            .OrderBy(a => a.Sala)
            .ThenBy(a => a.Ordem)
            .ThenBy(a => a.Nome)
            .Select(a => new AgenteAdminDto(
                a.Id, a.Slug, a.Nome, a.Papel, a.Sala.ToString(), a.Ordem, a.Ativo))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AgenteAdminDto>>(lista);
    }
}
