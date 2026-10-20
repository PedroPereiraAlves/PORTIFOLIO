using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PedroHub.Domain.Entities;
using PedroHub.Domain.Enums;
using PedroHub.Infrastructure.Persistence;

namespace PedroHub.Infrastructure.Escritorio;

public static class EscritorioSeed
{
    private static readonly (string Slug, string Nome, string Papel, SalaEscritorio Sala, int Ordem, string Cor, string Sprite)[] Elenco =
    [
        ("gideon", "Gideon", "Tech lead", SalaEscritorio.Central, 0, "#c9a227", "gideon"),
        ("backend-hub", "Backend Hub", "API e domínio", SalaEscritorio.Hub, 1, "#1e5a8a", "agent"),
        ("front-hub", "Front Hub", "UI do Hub", SalaEscritorio.Hub, 2, "#2a7ab8", "agent"),
        ("monitoramento-hub", "Monitoramento Hub", "Saúde e alertas", SalaEscritorio.Hub, 3, "#0d9488", "agent"),
        ("seguranca-hub", "Segurança Hub", "Auth e SSRF", SalaEscritorio.Hub, 4, "#b45309", "agent"),
        ("infra-testes-hub", "Infra e Testes Hub", "CI e infra", SalaEscritorio.Hub, 5, "#64748b", "agent"),
        ("backend-clinica", "Backend Clinica", "API da clínica", SalaEscritorio.Clinica, 1, "#1e5a8a", "agent"),
        ("front-clinica", "Front Clinica", "UI da clínica", SalaEscritorio.Clinica, 2, "#2a7ab8", "agent"),
        ("seguranca-clinica", "Segurança Clinica", "Auth clínica", SalaEscritorio.Clinica, 3, "#b45309", "agent"),
        ("infra-clinica", "Infra Clinica", "Deploy clínica", SalaEscritorio.Clinica, 4, "#64748b", "agent"),
        ("design-clinica", "Design Clinica", "UX/UI", SalaEscritorio.Clinica, 5, "#a855f7", "agent"),
        ("testes-clinica", "Testes Clinica", "QA", SalaEscritorio.Clinica, 6, "#059669", "agent"),
    ];

    public static async Task EnsureElencoAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("EscritorioSeed");

        var existentes = await db.AgentesEscritorio
            .Select(a => a.Slug)
            .ToListAsync();
        var set = existentes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var novos = 0;
        foreach (var e in Elenco)
        {
            if (set.Contains(e.Slug))
                continue;

            db.AgentesEscritorio.Add(new AgenteEscritorio
            {
                Slug = e.Slug,
                Nome = e.Nome,
                Papel = e.Papel,
                Sala = e.Sala,
                Ordem = e.Ordem,
                CorAvatar = e.Cor,
                SpriteKey = e.Sprite,
                Ativo = true
            });
            novos++;
        }

        if (novos == 0)
            return;

        await db.SaveChangesAsync();
        logger.LogInformation("Seed do escritório: {Novos} agentes criados.", novos);
    }
}
