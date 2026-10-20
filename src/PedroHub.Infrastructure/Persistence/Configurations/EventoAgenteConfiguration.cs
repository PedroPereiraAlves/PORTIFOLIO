using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedroHub.Domain.Entities;

namespace PedroHub.Infrastructure.Persistence.Configurations;

public sealed class EventoAgenteConfiguration : IEntityTypeConfiguration<EventoAgente>
{
    public void Configure(EntityTypeBuilder<EventoAgente> builder)
    {
        builder.ToTable("eventos_agente");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Titulo).HasMaxLength(140).IsRequired();
        builder.Property(x => x.Descricao).HasMaxLength(500);
        builder.Property(x => x.Link).HasMaxLength(300);
        builder.HasOne(x => x.Agente)
            .WithMany(x => x.Eventos)
            .HasForeignKey(x => x.AgenteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.AgenteId, x.OcorridoEm });
    }
}
