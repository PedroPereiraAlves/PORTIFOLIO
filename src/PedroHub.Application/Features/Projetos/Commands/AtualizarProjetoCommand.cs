using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Validation;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Projetos.Commands;

public sealed record AtualizarProjetoCommand(
    Guid Id,
    string? Descricao,
    string? UrlDemo,
    bool Visivel,
    bool Destaque,
    int Ordem) : IRequest<Result>;

public sealed class AtualizarProjetoCommandValidator : AbstractValidator<AtualizarProjetoCommand>
{
    public AtualizarProjetoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UrlDemo)
            .Must(UrlRules.EhHttpOuHttpsOpcional)
            .WithMessage("URL de demo deve ser http ou https.");
        RuleFor(x => x.Ordem).GreaterThanOrEqualTo(0);
    }
}

public sealed class AtualizarProjetoCommandHandler : IRequestHandler<AtualizarProjetoCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public AtualizarProjetoCommandHandler(IApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<Result> Handle(AtualizarProjetoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await _db.Projetos.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (projeto is null)
            return Result.Failure("Projeto não encontrado.");

        projeto.Descricao = request.Descricao?.Trim();
        projeto.UrlDemo = string.IsNullOrWhiteSpace(request.UrlDemo) ? null : request.UrlDemo.Trim();
        projeto.Visivel = request.Visivel;
        projeto.Destaque = request.Destaque;
        projeto.Ordem = request.Ordem;

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
