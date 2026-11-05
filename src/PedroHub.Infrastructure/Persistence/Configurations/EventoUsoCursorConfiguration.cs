using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedroHub.Domain.Entities;

namespace PedroHub.Infrastructure.Persistence.Configurations;

public sealed class EventoUsoCursorConfiguration : IEntityTypeConfiguration<EventoUsoCursor>
{
    public void Configure(EntityTypeBuilder<EventoUsoCursor> builder)
    {
        builder.ToTable("eventos_uso_cursor");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.Modelo).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Tipo).HasMaxLength(80);
        builder.Property(x => x.Origem).HasMaxLength(16).IsRequired();
        builder.Property(x => x.ChaveNatural).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CustoCentavos).HasPrecision(18, 6);
        builder.Property(x => x.UnidadesRequisicao).HasPrecision(18, 6);

        builder.HasIndex(x => x.ChaveNatural).IsUnique();
        builder.HasIndex(x => x.OcorridoEm);
        builder.HasIndex(x => new { x.Modelo, x.OcorridoEm });
    }
}
