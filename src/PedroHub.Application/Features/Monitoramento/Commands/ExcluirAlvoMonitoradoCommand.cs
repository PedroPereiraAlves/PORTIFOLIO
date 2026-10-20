using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Monitoramento.Commands;

public sealed record ExcluirAlvoMonitoradoCommand(Guid Id) : IRequest<Result>;

public sealed class ExcluirAlvoMonitoradoCommandHandler : IRequestHandler<ExcluirAlvoMonitoradoCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public ExcluirAlvoMonitoradoCommandHandler(IApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<Result> Handle(ExcluirAlvoMonitoradoCommand request, CancellationToken cancellationToken)
    {
        var alvo = await _db.AlvosMonitorados.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (alvo is null)
            return Result.Failure("Alvo não encontrado.");

        _db.AlvosMonitorados.Remove(alvo);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
