using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedroHub.Domain.Entities;

namespace PedroHub.Infrastructure.Persistence.Configurations;

public sealed class StatusAgenteAtualConfiguration : IEntityTypeConfiguration<StatusAgenteAtual>
{
    public void Configure(EntityTypeBuilder<StatusAgenteAtual> builder)
    {
        builder.ToTable("status_agente_atual");
        builder.HasKey(x => x.AgenteId);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.TarefaAtual).HasMaxLength(280);
        builder.Property(x => x.Projeto).HasMaxLength(60);
        builder.Property(x => x.PrUrl).HasMaxLength(300);
        builder.HasOne(x => x.Agente)
            .WithOne(x => x.StatusAtual)
            .HasForeignKey<StatusAgenteAtual>(x => x.AgenteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
