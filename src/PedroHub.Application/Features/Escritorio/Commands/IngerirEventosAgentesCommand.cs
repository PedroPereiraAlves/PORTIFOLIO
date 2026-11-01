using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Escritorio;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Escritorio.Commands;

public sealed record EventoAgenteIngestaoItem(
    string Slug,
    string Tipo,
    string Titulo,
    string? Descricao,
    string? Link,
    DateTime OcorridoEm);

public sealed record IngerirEventosAgentesCommand(IReadOnlyList<EventoAgenteIngestaoItem> Eventos)
    : IRequest<Result>;

public sealed class IngerirEventosAgentesCommandValidator : AbstractValidator<IngerirEventosAgentesCommand>
{
    public IngerirEventosAgentesCommandValidator()
    {
        RuleFor(x => x.Eventos).NotNull().Must(e => e.Count <= 20)
            .WithMessage("Máximo de 20 eventos por requisição.");
        RuleForEach(x => x.Eventos).ChildRules(item =>
        {
            item.RuleFor(i => i.Slug).NotEmpty().MaximumLength(80);
            item.RuleFor(i => i.Tipo)
                .Must(t => TipoEventoAgenteParser.TentarParse(t, out _))
                .WithMessage("tipo inválido.");
            item.RuleFor(i => i.Titulo).NotEmpty().MaximumLength(140);
            item.RuleFor(i => i.Descricao).MaximumLength(500);
            item.RuleFor(i => i.Link)
                .Must(GithubPrUrlRules.EhPermitida)
                .WithMessage("link deve ser https://github.com/PedroPereiraAlves/...");
            item.RuleFor(i => i.OcorridoEm)
                .Must(d => d <= DateTime.UtcNow.AddMinutes(5))
                .WithMessage("ocorridoEm não pode estar mais de 5 min no futuro.");
        });
    }
}

public sealed class IngerirEventosAgentesCommandHandler : IRequestHandler<IngerirEventosAgentesCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public IngerirEventosAgentesCommandHandler(IApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<Result> Handle(IngerirEventosAgentesCommand request, CancellationToken cancellationToken)
    {
        if (request.Eventos.Count == 0)
            return Result.Success();

        var slugs = request.Eventos
            .Select(e => e.Slug.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
        var agentes = await _db.AgentesEscritorio
            .Where(a => slugs.Contains(a.Slug))
            .ToListAsync(cancellationToken);
        var porSlug = agentes.ToDictionary(a => a.Slug, StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Eventos)
        {
            if (!porSlug.TryGetValue(item.Slug.Trim(), out var agente))
                return Result.Failure($"slug inexistente: {item.Slug}");

            TipoEventoAgenteParser.TentarParse(item.Tipo, out var tipo);
            var ocorrido = item.OcorridoEm.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(item.OcorridoEm, DateTimeKind.Utc)
                : item.OcorridoEm.ToUniversalTime();

            _db.EventosAgente.Add(new EventoAgente
            {
                AgenteId = agente.Id,
                Tipo = tipo,
                Titulo = item.Titulo.Trim(),
                Descricao = string.IsNullOrWhiteSpace(item.Descricao) ? null : item.Descricao.Trim(),
                Link = string.IsNullOrWhiteSpace(item.Link) ? null : item.Link.Trim(),
                OcorridoEm = ocorrido
            });
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
