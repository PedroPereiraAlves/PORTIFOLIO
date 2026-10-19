using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PedroHub.Domain.Common;

namespace PedroHub.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AtualizarAuditoria(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AtualizarAuditoria(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void AtualizarAuditoria(DbContext? context)
    {
        if (context is null)
            return;

        var agora = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CriadoEm = agora;
                entry.Entity.AtualizadoEm = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.AtualizadoEm = agora;
            }
        }
    }
}
