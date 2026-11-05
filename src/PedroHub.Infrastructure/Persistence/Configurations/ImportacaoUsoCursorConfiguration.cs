using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedroHub.Domain.Entities;

namespace PedroHub.Infrastructure.Persistence.Configurations;

public sealed class ImportacaoUsoCursorConfiguration : IEntityTypeConfiguration<ImportacaoUsoCursor>
{
    public void Configure(EntityTypeBuilder<ImportacaoUsoCursor> builder)
    {
        builder.ToTable("importacoes_uso_cursor");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Origem).HasMaxLength(16).IsRequired();
        builder.Property(x => x.NomeArquivo).HasMaxLength(260);
        builder.Property(x => x.Aviso).HasMaxLength(500);
        builder.HasIndex(x => x.CriadoEm);
    }
}
