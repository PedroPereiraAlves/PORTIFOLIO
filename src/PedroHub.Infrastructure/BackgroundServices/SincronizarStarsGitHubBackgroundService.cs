using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PedroHub.Application.Features.Sync.Commands;

namespace PedroHub.Infrastructure.BackgroundServices;

public sealed class SincronizarStarsGitHubBackgroundService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SincronizarStarsGitHubBackgroundService> _logger;

    public SincronizarStarsGitHubBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SincronizarStarsGitHubBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Aguarda um pouco no boot para migrations/seed
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new SincronizarStarsGitHubCommand(), stoppingToken);

                if (result.IsFailure)
                    _logger.LogWarning("Sync periódica falhou: {Erro}", result.Error);
                else
                    _logger.LogInformation(
                        "Sync periódica OK. Criados={Criados}, Atualizados={Atualizados}",
                        result.Value.Criados,
                        result.Value.Atualizados);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado na sync periódica de stars.");
            }

            try
            {
                await Task.Delay(Intervalo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
