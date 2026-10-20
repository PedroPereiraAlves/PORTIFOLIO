using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedroHub.Domain.Entities;

namespace PedroHub.Infrastructure.Persistence.Configurations;

public sealed class AlvoMonitoradoConfiguration : IEntityTypeConfiguration<AlvoMonitorado>
{
    public void Configure(EntityTypeBuilder<AlvoMonitorado> builder)
    {
        builder.ToTable("alvos_monitorados");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Metodo).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.StatusEsperado).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Infra).HasMaxLength(100);
        builder.Property(x => x.StatusAtual).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(x => x.Projeto)
            .WithMany()
            .HasForeignKey(x => x.ProjetoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.ProximaChecagemEm);
        builder.HasIndex(x => new { x.Ativo, x.ProximaChecagemEm });
        builder.HasIndex(x => x.Url);
    }
}
