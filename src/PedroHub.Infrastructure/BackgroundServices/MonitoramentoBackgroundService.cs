using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PedroHub.Application.Common.Options;
using PedroHub.Application.Features.Monitoramento.Commands;
using PedroHub.Infrastructure.Persistence;

namespace PedroHub.Infrastructure.BackgroundServices;

public sealed class MonitoramentoBackgroundService : BackgroundService
{
    private static readonly TimeSpan Ciclo = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonitoramentoBackgroundService> _logger;
    private DateTime _ultimaRetencao = DateTime.MinValue;

    public MonitoramentoBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MonitoramentoBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var inicio = DateTime.UtcNow;
            try
            {
                await ExecutarCicloAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo de monitoramento. TraceId={TraceId}",
                    System.Diagnostics.Activity.Current?.Id);
            }

            var duracao = DateTime.UtcNow - inicio;
            _logger.LogDebug(
                "Ciclo monitoramento encerrado. DuracaoMs={Duracao} TraceId={TraceId}",
                (int)duracao.TotalMilliseconds,
                System.Diagnostics.Activity.Current?.Id);

            try
            {
                await Task.Delay(Ciclo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ExecutarCicloAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<MonitoramentoOptions>>().Value;

        var agora = DateTime.UtcNow;
        await AplicarRetencaoSeNecessarioAsync(db, options, agora, stoppingToken);

        var devidos = await db.AlvosMonitorados
            .AsNoTracking()
            .Where(a => a.Ativo && (a.ProximaChecagemEm == null || a.ProximaChecagemEm <= agora))
            .Select(a => a.Id)
            .ToListAsync(stoppingToken);

        if (devidos.Count == 0)
            return;

        var limite = Math.Clamp(options.ConcorrenciaMaxima, 1, 16);
        using var sem = new SemaphoreSlim(limite);
        var tarefas = devidos.Select(async id =>
        {
            await sem.WaitAsync(stoppingToken);
            try
            {
                using var inner = _scopeFactory.CreateScope();
                var med = inner.ServiceProvider.GetRequiredService<IMediator>();
                var result = await med.Send(new ExecutarChecagemCommand(id), stoppingToken);
                if (result.IsFailure)
                    _logger.LogWarning("Checagem falhou. AlvoId={AlvoId} Erro={Erro}", id, result.Error);
            }
            finally
            {
                sem.Release();
            }
        });

        await Task.WhenAll(tarefas);
    }

    private async Task AplicarRetencaoSeNecessarioAsync(
        ApplicationDbContext db,
        MonitoramentoOptions options,
        DateTime agora,
        CancellationToken cancellationToken)
    {
        if (agora - _ultimaRetencao < TimeSpan.FromDays(1))
            return;

        var dias = Math.Clamp(options.RetencaoDias, 1, 365);
        var corte = agora.AddDays(-dias);
        const int lote = 500;

        int removidos;
        do
        {
            var ids = await db.Checagens
                .Where(c => c.ExecutadaEm < corte)
                .OrderBy(c => c.ExecutadaEm)
                .Take(lote)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            if (ids.Count == 0)
                break;

            await db.Checagens.Where(c => ids.Contains(c.Id)).ExecuteDeleteAsync(cancellationToken);
            removidos = ids.Count;
            _logger.LogInformation("Retenção de checagens: removidos={Removidos} corte={Corte}", removidos, corte);
        } while (removidos == lote);

        _ultimaRetencao = agora;
    }
}
