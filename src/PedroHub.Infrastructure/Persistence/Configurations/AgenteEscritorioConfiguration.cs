using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedroHub.Domain.Entities;

namespace PedroHub.Infrastructure.Persistence.Configurations;

public sealed class AgenteEscritorioConfiguration : IEntityTypeConfiguration<AgenteEscritorio>
{
    public void Configure(EntityTypeBuilder<AgenteEscritorio> builder)
    {
        builder.ToTable("agentes_escritorio");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Nome).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Papel).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Sala).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CorAvatar).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SpriteKey).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.Sala, x.Ordem });
    }
}
