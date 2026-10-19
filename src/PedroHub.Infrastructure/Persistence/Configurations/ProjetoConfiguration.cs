using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedroHub.Domain.Entities;

namespace PedroHub.Infrastructure.Persistence.Configurations;

public sealed class ProjetoConfiguration : IEntityTypeConfiguration<Projeto>
{
    public void Configure(EntityTypeBuilder<Projeto> builder)
    {
        builder.ToTable("projetos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Descricao).HasMaxLength(4000);
        builder.Property(x => x.UrlRepositorio).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UrlDemo).HasMaxLength(500);
        builder.Property(x => x.Linguagem).HasMaxLength(100);
        builder.Property(x => x.Topicos).HasMaxLength(2000);
        builder.Property(x => x.Origem).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => x.UrlRepositorio).IsUnique();
        builder.HasIndex(x => new { x.Visivel, x.Destaque, x.Ordem });
    }
}
