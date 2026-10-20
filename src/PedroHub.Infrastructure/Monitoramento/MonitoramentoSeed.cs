using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Infrastructure.Persistence;

namespace PedroHub.Infrastructure.Monitoramento;

public static class MonitoramentoSeed
{
    private const string UrlAlvo = "https://site-rms-logistica.pages.dev";
    private const string UrlRepo = "https://github.com/PedroPereiraAlves/SITE-RMS-LOGISTICA";

    public static async Task EnsureSiteRmsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("MonitoramentoSeed");

        if (!env.IsDevelopment())
            return;

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existe = await db.AlvosMonitorados.AnyAsync(a =>
            a.Url == UrlAlvo || a.Url == UrlAlvo + "/");
        if (existe)
            return;

        var repoNorm = UrlRepo.TrimEnd('/').ToLowerInvariant();
        var projeto = await db.Projetos.FirstOrDefaultAsync(p =>
            p.UrlRepositorio.ToLower() == repoNorm
            || p.UrlRepositorio.ToLower() == repoNorm + "/");

        if (projeto is null)
            logger.LogInformation(
                "Seed SITE-RMS-LOGISTICA: projeto GitHub não encontrado; criando alvo sem vínculo.");

        var agora = DateTime.UtcNow;
        db.AlvosMonitorados.Add(new AlvoMonitorado
        {
            Nome = "SITE-RMS-LOGISTICA",
            Url = UrlAlvo,
            Metodo = MetodoChecagem.Get,
            IntervaloSegundos = 300,
            TimeoutMs = 10_000,
            StatusEsperado = "200",
            LimiarDegradadoMs = 1500,
            Ativo = true,
            Infra = "Cloudflare Pages",
            ProjetoId = projeto?.Id,
            ProximaChecagemEm = agora
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Seed SITE-RMS-LOGISTICA criado. ProjetoId={ProjetoId}", projeto?.Id);
    }
}
