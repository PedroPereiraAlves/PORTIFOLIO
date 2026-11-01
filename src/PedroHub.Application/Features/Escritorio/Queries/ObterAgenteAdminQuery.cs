using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.Escritorio.Dtos;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Escritorio.Queries;

public sealed record ObterAgenteAdminQuery(Guid Id) : IRequest<Result<AgenteAdminDto>>;

public sealed class ObterAgenteAdminQueryHandler
    : IRequestHandler<ObterAgenteAdminQuery, Result<AgenteAdminDto>>
{
    private readonly IApplicationDbContext _db;

    public ObterAgenteAdminQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<AgenteAdminDto>> Handle(
        ObterAgenteAdminQuery request,
        CancellationToken cancellationToken)
    {
        var a = await _db.AgentesEscritorio
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new AgenteAdminDto(
                x.Id, x.Slug, x.Nome, x.Papel, x.Sala.ToString(), x.Ordem, x.Ativo))
            .FirstOrDefaultAsync(cancellationToken);

        return a is null
            ? Result.Failure<AgenteAdminDto>("Agente não encontrado.")
            : Result.Success(a);
    }
}