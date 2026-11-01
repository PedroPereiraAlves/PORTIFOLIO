using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Domain.Escritorio;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Escritorio.Commands;

public sealed record StatusAgenteIngestaoItem(
    string Slug,
    string Status,
    string? TarefaAtual,
    string? Projeto,
    string? PrUrl,
    DateTime AtualizadoEm);

public sealed record IngerirStatusAgentesCommand(IReadOnlyList<StatusAgenteIngestaoItem> Agentes)
    : IRequest<Result>;

public sealed class IngerirStatusAgentesCommandValidator : AbstractValidator<IngerirStatusAgentesCommand>
{
    public IngerirStatusAgentesCommandValidator()
    {
        RuleFor(x => x.Agentes).NotNull().Must(a => a.Count <= 20)
            .WithMessage("Máximo de 20 agentes por requisição.");
        RuleForEach(x => x.Agentes).ChildRules(item =>
        {
            item.RuleFor(i => i.Slug).NotEmpty().MaximumLength(80);
            item.RuleFor(i => i.Status)
                .Must(s => StatusAgenteParser.TentarParse(s, out _))
                .WithMessage("status inválido.");
            item.RuleFor(i => i.TarefaAtual).MaximumLength(280)
                .Must(t => t is null || t.All(c => !char.IsControl(c) || c is '\n' or '\r' or '\t'))
                .WithMessage("tarefaAtual deve ser texto puro.");
            item.RuleFor(i => i.Projeto)
                .Must(p => string.IsNullOrWhiteSpace(p) || p.Length <= 60)
                .WithMessage("projeto deve ter no máximo 60 caracteres.")
                .Must(p =>
                {
                    if (string.IsNullOrWhiteSpace(p)) return true;
                    var n = p.Trim();
                    return n.Equals("Hub", StringComparison.OrdinalIgnoreCase)
                           || n.Equals("Clinica", StringComparison.OrdinalIgnoreCase)
                           || n.Equals("outro", StringComparison.OrdinalIgnoreCase)
                           || n.Length <= 60;
                })
                .WithMessage("projeto deve ser Hub, Clinica, outro ou texto ≤ 60.");
            item.RuleFor(i => i.PrUrl)
                .Must(GithubPrUrlRules.EhPermitida)
                .WithMessage("prUrl deve ser https://github.com/PedroPereiraAlves/...");
            item.RuleFor(i => i.AtualizadoEm)
                .Must(d => d <= DateTime.UtcNow.AddMinutes(5))
                .WithMessage("atualizadoEm não pode estar mais de 5 min no futuro.");
        });
    }
}

public sealed class IngerirStatusAgentesCommandHandler : IRequestHandler<IngerirStatusAgentesCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public IngerirStatusAgentesCommandHandler(IApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<Result> Handle(IngerirStatusAgentesCommand request, CancellationToken cancellationToken)
    {
        if (request.Agentes.Count == 0)
            return Result.Success();

        var slugs = request.Agentes
            .Select(a => a.Slug.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
        var agentes = await _db.AgentesEscritorio
            .Include(a => a.StatusAtual)
            .Where(a => slugs.Contains(a.Slug))
            .ToListAsync(cancellationToken);

        var porSlug = agentes.ToDictionary(a => a.Slug, StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Agentes)
        {
            if (!porSlug.TryGetValue(item.Slug.Trim(), out var agente))
                return Result.Failure($"slug inexistente: {item.Slug}");

            StatusAgenteParser.TentarParse(item.Status, out var status);
            var publicado = item.AtualizadoEm.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(item.AtualizadoEm, DateTimeKind.Utc)
                : item.AtualizadoEm.ToUniversalTime();

            if (agente.StatusAtual is { } atual && publicado < atual.AtualizadoEmPublicado)
                continue;

            var tarefa = string.IsNullOrWhiteSpace(item.TarefaAtual) ? null : item.TarefaAtual.Trim();
            var projeto = string.IsNullOrWhiteSpace(item.Projeto) ? null : item.Projeto.Trim();
            var pr = string.IsNullOrWhiteSpace(item.PrUrl) ? null : item.PrUrl.Trim();

            if (agente.StatusAtual is null)
            {
                agente.StatusAtual = new StatusAgenteAtual
                {
                    AgenteId = agente.Id,
                    Status = status,
                    TarefaAtual = tarefa,
                    Projeto = projeto,
                    PrUrl = pr,
                    AtualizadoEmPublicado = publicado
                };
                _db.StatusAgentesAtual.Add(agente.StatusAtual);
            }
            else
            {
                agente.StatusAtual.Status = status;
                agente.StatusAtual.TarefaAtual = tarefa;
                agente.StatusAtual.Projeto = projeto;
                agente.StatusAtual.PrUrl = pr;
                agente.StatusAtual.AtualizadoEmPublicado = publicado;
            }

            _db.StatusAgentesHistorico.Add(new StatusAgenteHistorico
            {
                AgenteId = agente.Id,
                Status = status,
                TarefaAtual = tarefa,
                Projeto = projeto,
                PrUrl = pr,
                PublicadoEm = publicado
            });
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
