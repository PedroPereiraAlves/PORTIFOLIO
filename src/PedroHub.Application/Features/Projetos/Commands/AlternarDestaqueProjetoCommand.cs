using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Projetos.Commands;

public sealed record AlternarDestaqueProjetoCommand(Guid Id) : IRequest<Result>;

public sealed class AlternarDestaqueProjetoCommandHandler
    : IRequestHandler<AlternarDestaqueProjetoCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public AlternarDestaqueProjetoCommandHandler(IApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<Result> Handle(AlternarDestaqueProjetoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await _db.Projetos.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (projeto is null)
            return Result.Failure("Projeto não encontrado.");

        projeto.Destaque = !projeto.Destaque;
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
