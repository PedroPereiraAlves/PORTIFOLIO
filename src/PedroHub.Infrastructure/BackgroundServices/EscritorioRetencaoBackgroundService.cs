using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PedroHub.Application.Features.Escritorio.Commands;

namespace PedroHub.Infrastructure.BackgroundServices;

public sealed class EscritorioRetencaoBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EscritorioRetencaoBackgroundService> _logger;
    private DateTime _ultimaRetencao = DateTime.MinValue;

    public EscritorioRetencaoBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EscritorioRetencaoBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var agora = DateTime.UtcNow;
                if (agora - _ultimaRetencao >= TimeSpan.FromDays(1))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    var result = await mediator.Send(new AplicarRetencaoEscritorioCommand(), stoppingToken);
                    if (result.IsSuccess)
                        _ultimaRetencao = agora;
                    else
                        _logger.LogWarning("Retenção escritório falhou: {Erro}", result.Error);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo de retenção do escritório.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
