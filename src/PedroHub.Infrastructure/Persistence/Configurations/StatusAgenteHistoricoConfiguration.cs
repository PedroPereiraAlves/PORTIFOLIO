using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedroHub.Domain.Entities;

namespace PedroHub.Infrastructure.Persistence.Configurations;

public sealed class StatusAgenteHistoricoConfiguration : IEntityTypeConfiguration<StatusAgenteHistorico>
{
    public void Configure(EntityTypeBuilder<StatusAgenteHistorico> builder)
    {
        builder.ToTable("status_agente_historico");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.TarefaAtual).HasMaxLength(280);
        builder.Property(x => x.Projeto).HasMaxLength(60);
        builder.Property(x => x.PrUrl).HasMaxLength(300);
        builder.HasOne(x => x.Agente)
            .WithMany(x => x.Historico)
            .HasForeignKey(x => x.AgenteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.AgenteId, x.PublicadoEm });
    }
}
