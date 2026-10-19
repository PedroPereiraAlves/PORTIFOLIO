using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Monitoramento.Commands;

public sealed record AlternarAtivoAlvoCommand(Guid Id) : IRequest<Result>;

public sealed class AlternarAtivoAlvoCommandHandler : IRequestHandler<AlternarAtivoAlvoCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public AlternarAtivoAlvoCommandHandler(IApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<Result> Handle(AlternarAtivoAlvoCommand request, CancellationToken cancellationToken)
    {
        var alvo = await _db.AlvosMonitorados.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (alvo is null)
            return Result.Failure("Alvo não encontrado.");

        alvo.Ativo = !alvo.Ativo;
        if (alvo.Ativo)
            alvo.ProximaChecagemEm = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
