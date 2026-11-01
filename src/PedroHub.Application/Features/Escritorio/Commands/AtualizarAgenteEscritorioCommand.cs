using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Domain.Enums;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Escritorio.Commands;

public sealed record AtualizarAgenteEscritorioCommand(
    Guid Id,
    string Nome,
    string Papel,
    SalaEscritorio Sala,
    int Ordem,
    bool Ativo) : IRequest<Result>;

public sealed class AtualizarAgenteEscritorioCommandValidator : AbstractValidator<AtualizarAgenteEscritorioCommand>
{
    public AtualizarAgenteEscritorioCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Papel).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Sala).IsInEnum();
        RuleFor(x => x.Ordem).InclusiveBetween(0, 999);
    }
}

public sealed class AtualizarAgenteEscritorioCommandHandler
    : IRequestHandler<AtualizarAgenteEscritorioCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public AtualizarAgenteEscritorioCommandHandler(IApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<Result> Handle(AtualizarAgenteEscritorioCommand request, CancellationToken cancellationToken)
    {
        var agente = await _db.AgentesEscritorio
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (agente is null)
            return Result.Failure("Agente não encontrado.");

        agente.Nome = request.Nome.Trim();
        agente.Papel = request.Papel.Trim();
        agente.Sala = request.Sala;
        agente.Ordem = request.Ordem;
        agente.Ativo = request.Ativo;

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
