using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.Projetos.Dtos;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Projetos.Queries;

public sealed record ObterProjetoPorIdQuery(Guid Id) : IRequest<Result<ProjetoDto>>;

public sealed class ObterProjetoPorIdQueryHandler
    : IRequestHandler<ObterProjetoPorIdQuery, Result<ProjetoDto>>
{
    private readonly IApplicationDbContext _db;

    public ObterProjetoPorIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ProjetoDto>> Handle(
        ObterProjetoPorIdQuery request,
        CancellationToken cancellationToken)
    {
        var projeto = await _db.Projetos
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);

        return projeto is null
            ? Result.Failure<ProjetoDto>("Projeto não encontrado.")
            : Result.Success(projeto);
    }
}
