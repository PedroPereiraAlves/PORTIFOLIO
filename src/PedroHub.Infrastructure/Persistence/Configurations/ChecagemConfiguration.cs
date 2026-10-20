using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedroHub.Domain.Entities;

namespace PedroHub.Infrastructure.Persistence.Configurations;

public sealed class ChecagemConfiguration : IEntityTypeConfiguration<Checagem>
{
    public void Configure(EntityTypeBuilder<Checagem> builder)
    {
        builder.ToTable("checagens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Erro).HasMaxLength(500);

        builder.HasOne(x => x.AlvoMonitorado)
            .WithMany(x => x.Checagens)
            .HasForeignKey(x => x.AlvoMonitoradoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.AlvoMonitoradoId, x.ExecutadaEm });
    }
}
