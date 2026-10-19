using MediatR;
using Microsoft.EntityFrameworkCore;
using PedroHub.Application.Common.Interfaces;
using PedroHub.Application.Features.Monitoramento.Dtos;
using PedroHub.Domain.Enums;
using PedroHub.Domain.Monitoramento;
using PedroHub.Shared.Results;

namespace PedroHub.Application.Features.Monitoramento.Queries;

public sealed record ObterResumoMonitoramentoQuery(string Periodo = "24h", Guid? AlvoIdSerie = null)
    : IRequest<Result<ResumoMonitoramentoDto>>;

public sealed class ObterResumoMonitoramentoQueryHandler
    : IRequestHandler<ObterResumoMonitoramentoQuery, Result<ResumoMonitoramentoDto>>
{
    private readonly IApplicationDbContext _db;

    public ObterResumoMonitoramentoQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ResumoMonitoramentoDto>> Handle(
        ObterResumoMonitoramentoQuery request,
        CancellationToken cancellationToken)
    {
        var agora = DateTime.UtcNow;
        var periodo = NormalizarPeriodo(request.Periodo);
        var desdeSerie = agora - PeriodoParaTimeSpan(periodo);

        var alvos = await _db.AlvosMonitorados
            .AsNoTracking()
            .Include(a => a.Projeto)
            .OrderBy(a => a.Nome)
            .ToListAsync(cancellationToken);

        var ids = alvos.Select(a => a.Id).ToList();
        var desde30d = agora.AddDays(-30);

        var checagens = await _db.Checagens
            .AsNoTracking()
            .Where(c => ids.Contains(c.AlvoMonitoradoId) && c.ExecutadaEm >= desde30d)
            .Select(c => new { c.AlvoMonitoradoId, c.ExecutadaEm, c.Status, c.LatenciaMs })
            .ToListAsync(cancellationToken);

        var dtos = new List<AlvoMonitoradoDto>(alvos.Count);
        foreach (var alvo in alvos)
        {
            var doAlvo = checagens.Where(c => c.AlvoMonitoradoId == alvo.Id).ToList();
            var s24 = doAlvo.Where(c => c.ExecutadaEm >= agora.AddHours(-24)).Select(c => c.Status).ToList();
            var s7 = doAlvo.Where(c => c.ExecutadaEm >= agora.AddDays(-7)).Select(c => c.Status).ToList();
            var s30 = doAlvo.Select(c => c.Status).ToList();
            var latencias30 = doAlvo.Select(c => c.LatenciaMs).ToList();
            var (p50, p95) = MetricasMonitoramento.CalcularPercentisLatencia(latencias30);
            var ultimaLatencia = doAlvo
                .OrderByDescending(c => c.ExecutadaEm)
                .Select(c => (int?)c.LatenciaMs)
                .FirstOrDefault();

            dtos.Add(new AlvoMonitoradoDto(
                alvo.Id,
                alvo.Nome,
                alvo.Url,
                alvo.Metodo,
                alvo.IntervaloSegundos,
                alvo.TimeoutMs,
                alvo.StatusEsperado,
                alvo.LimiarDegradadoMs,
                alvo.Ativo,
                alvo.ProjetoId,
                alvo.Projeto?.Nome,
                alvo.Infra,
                alvo.StatusAtual,
                alvo.FalhasConsecutivas,
                alvo.UltimaChecagemEm,
                alvo.ProximaChecagemEm,
                alvo.UltimaMudancaStatusEm,
                ultimaLatencia,
                MetricasMonitoramento.CalcularUptime(s24),
                MetricasMonitoramento.CalcularUptime(s7),
                MetricasMonitoramento.CalcularUptime(s30),
                p50,
                p95));
        }

        var alvoSerieId = request.AlvoIdSerie
            ?? alvos.FirstOrDefault(a => a.Ativo)?.Id
            ?? alvos.FirstOrDefault()?.Id;

        IReadOnlyList<PontoLatenciaDto> serie = [];
        if (alvoSerieId is Guid serieId)
        {
            var pontos = checagens
                .Where(c => c.AlvoMonitoradoId == serieId && c.ExecutadaEm >= desdeSerie)
                .OrderBy(c => c.ExecutadaEm)
                .ToList();

            serie = AgregarSerie(pontos.Select(p => (p.ExecutadaEm, p.LatenciaMs, p.Status)).ToList(), periodo);
        }

        var ultimaExec = alvos.Where(a => a.UltimaChecagemEm.HasValue).Max(a => a.UltimaChecagemEm);
        var proximaExec = alvos.Where(a => a.Ativo && a.ProximaChecagemEm.HasValue).Min(a => a.ProximaChecagemEm);

        return Result.Success(new ResumoMonitoramentoDto(dtos, ultimaExec, proximaExec, serie, periodo));
    }

    private static string NormalizarPeriodo(string? periodo) =>
        periodo?.Trim().ToLowerInvariant() switch
        {
            "7d" => "7d",
            "30d" => "30d",
            _ => "24h"
        };

    private static TimeSpan PeriodoParaTimeSpan(string periodo) =>
        periodo switch
        {
            "7d" => TimeSpan.FromDays(7),
            "30d" => TimeSpan.FromDays(30),
            _ => TimeSpan.FromHours(24)
        };

    private static IReadOnlyList<PontoLatenciaDto> AgregarSerie(
        IReadOnlyList<(DateTime Em, int LatenciaMs, StatusAlvo Status)> pontos,
        string periodo)
    {
        if (pontos.Count == 0)
            return [];

        var bucket = periodo switch
        {
            "7d" => TimeSpan.FromHours(1),
            "30d" => TimeSpan.FromHours(6),
            _ => TimeSpan.FromMinutes(5)
        };

        return pontos
            .GroupBy(p => new DateTime(
                p.Em.Ticks - (p.Em.Ticks % bucket.Ticks),
                DateTimeKind.Utc))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var latencias = g.Select(x => x.LatenciaMs).OrderBy(x => x).ToList();
                var status = g.Any(x => x.Status == StatusAlvo.Offline)
                    ? StatusAlvo.Offline
                    : g.Any(x => x.Status == StatusAlvo.Degradado)
                        ? StatusAlvo.Degradado
                        : StatusAlvo.Online;
                return new PontoLatenciaDto(g.Key, MetricasMonitoramento.Percentil(latencias, 0.50), status);
            })
            .ToList();
    }
}
