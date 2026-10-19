using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Validation;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Projetos.Commands;

public sealed record CriarProjetoManualCommand(
    string Nome,
    string? Descricao,
    string UrlRepositorio,
    string? UrlDemo,
    string? Linguagem,
    bool Visivel,
    bool Destaque,
    int Ordem) : IRequest<Result<Guid>>;

public sealed class CriarProjetoManualCommandValidator : AbstractValidator<CriarProjetoManualCommand>
{
    public CriarProjetoManualCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UrlRepositorio).NotEmpty().MaximumLength(500)
            .Must(UrlRules.EhHttpOuHttps)
            .WithMessage("URL do repositório deve ser http ou https.");
        RuleFor(x => x.UrlDemo)
            .Must(UrlRules.EhHttpOuHttpsOpcional)
            .WithMessage("URL de demo deve ser http ou https.");
        RuleFor(x => x.Ordem).GreaterThanOrEqualTo(0);
    }
}

public sealed class CriarProjetoManualCommandHandler : IRequestHandler<CriarProjetoManualCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public CriarProjetoManualCommandHandler(IApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(CriarProjetoManualCommand request, CancellationToken cancellationToken)
    {
        var urlNormalizada = request.UrlRepositorio.Trim().TrimEnd('/');
        var existe = await _db.Projetos.AnyAsync(
            p => p.UrlRepositorio == urlNormalizada,
            cancellationToken);

        if (existe)
            return Result.Failure<Guid>("Já existe um projeto com esta URL de repositório.");

        var projeto = new Projeto
        {
            Nome = request.Nome.Trim(),
            Descricao = request.Descricao?.Trim(),
            UrlRepositorio = urlNormalizada,
            UrlDemo = string.IsNullOrWhiteSpace(request.UrlDemo) ? null : request.UrlDemo.Trim(),
            Linguagem = request.Linguagem?.Trim(),
            Visivel = request.Visivel,
            Destaque = request.Destaque,
            Ordem = request.Ordem,
            Origem = OrigemProjeto.Manual,
            Stars = 0
        };

        _db.Projetos.Add(projeto);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success(projeto.Id);
    }
}
