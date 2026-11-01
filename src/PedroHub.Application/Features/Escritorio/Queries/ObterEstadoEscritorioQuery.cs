using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Common.Options;
using PedroHub.Application.Features.Escritorio.Dtos;
using PedroHub.Domain.Escritorio;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Escritorio.Queries;

public sealed record ObterEstadoEscritorioQuery : IRequest<Result<EscritorioEstadoDto>>;

public sealed class ObterEstadoEscritorioQueryHandler
    : IRequestHandler<ObterEstadoEscritorioQuery, Result<EscritorioEstadoDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly EscritorioOptions _options;

    public ObterEstadoEscritorioQueryHandler(
        IApplicationDbContext db,
        IOptions<EscritorioOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<Result<EscritorioEstadoDto>> Handle(
        ObterEstadoEscritorioQuery request,
        CancellationToken cancellationToken)
    {
        var minutosDesat = Math.Clamp(_options.MinutosDesatualizado, 1, 24 * 60);
        var polling = Math.Clamp(_options.PollingSegundos, 5, 300);
        var agora = DateTime.UtcNow;
        var corteDesat = agora.AddMinutes(-minutosDesat);

        var agentes = await _db.AgentesEscritorio
            .AsNoTracking()
            .Where(a => a.Ativo)
            .OrderBy(a => a.Sala)
            .ThenBy(a => a.Ordem)
            .ThenBy(a => a.Nome)
            .Select(a => new
            {
                a.Id,
                a.Slug,
                a.Nome,
                a.Papel,
                a.Sala,
                a.CorAvatar,
                Status = a.StatusAtual != null ? a.StatusAtual.Status : (Domain.Enums.StatusAgenteEscritorio?)null,
                Tarefa = a.StatusAtual != null ? a.StatusAtual.TarefaAtual : null,
                Projeto = a.StatusAtual != null ? a.StatusAtual.Projeto : null,
                PrUrl = a.StatusAtual != null ? a.StatusAtual.PrUrl : null,
                AtualizadoEm = a.StatusAtual != null ? (DateTime?)a.StatusAtual.AtualizadoEmPublicado : null
            })
            .ToListAsync(cancellationToken);

        var ids = agentes.Select(a => a.Id).ToList();
        var eventos = await _db.EventosAgente
            .AsNoTracking()
            .Where(e => ids.Contains(e.AgenteId))
            .OrderByDescending(e => e.OcorridoEm)
            .Select(e => new { e.AgenteId, e.Tipo, e.Titulo, e.Descricao, e.Link, e.OcorridoEm })
            .ToListAsync(cancellationToken);

        var eventosPorAgente = eventos
            .GroupBy(e => e.AgenteId)
            .ToDictionary(
                g => g.Key,
                g => g.Take(5)
                    .Select(e => new EventoAgenteDto(
                        TipoEventoAgenteParser.ParaWire(e.Tipo),
                        e.Titulo,
                        e.Descricao,
                        e.Link,
                        e.OcorridoEm))
                    .ToList());

        var dtos = agentes.Select(a =>
        {
            var desatualizado = a.AtualizadoEm is null || a.AtualizadoEm < corteDesat;
            var status = a.Status is { } s
                ? StatusAgenteParser.ParaWire(s)
                : "ocioso";
            return new AgenteEstadoDto(
                a.Slug,
                a.Nome,
                a.Papel,
                a.Sala.ToString(),
                a.CorAvatar,
                status,
                a.Tarefa,
                a.Projeto,
                a.PrUrl,
                a.AtualizadoEm,
                FormatarHaQuantoTempo(a.AtualizadoEm, agora),
                desatualizado,
                eventosPorAgente.TryGetValue(a.Id, out var ev)
                    ? ev
                    : Array.Empty<EventoAgenteDto>());
        }).ToList();

        return Result.Success(new EscritorioEstadoDto(polling, dtos));
    }

    public static string FormatarHaQuantoTempo(DateTime? atualizadoEm, DateTime agora)
    {
        if (atualizadoEm is null)
            return "sem atualização";

        var delta = agora - atualizadoEm.Value;
        if (delta < TimeSpan.Zero)
            delta = TimeSpan.Zero;

        if (delta.TotalMinutes < 1)
            return "agora";
        if (delta.TotalMinutes < 60)
            return $"há {(int)delta.TotalMinutes} min";
        if (delta.TotalHours < 48)
            return $"há {(int)delta.TotalHours} h";
        return $"há {(int)delta.TotalDays} d";
    }
}
