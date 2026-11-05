using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedroHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumoCursor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_uso_cursor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OcorridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Modelo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ModoMaximo = table.Column<bool>(type: "boolean", nullable: false),
                    TokensEntrada = table.Column<long>(type: "bigint", nullable: false),
                    TokensCacheEscrita = table.Column<long>(type: "bigint", nullable: false),
                    TokensCacheLeitura = table.Column<long>(type: "bigint", nullable: false),
                    TokensSaida = table.Column<long>(type: "bigint", nullable: false),
                    TokensTotal = table.Column<long>(type: "bigint", nullable: false),
                    CustoCentavos = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    UnidadesRequisicao = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Origem = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ChaveNatural = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_uso_cursor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "importacoes_uso_cursor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Origem = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    NomeArquivo = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    LinhasLidas = table.Column<int>(type: "integer", nullable: false),
                    Inseridos = table.Column<int>(type: "integer", nullable: false),
                    Duplicados = table.Column<int>(type: "integer", nullable: false),
                    Ignorados = table.Column<int>(type: "integer", nullable: false),
                    Aviso = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_importacoes_uso_cursor", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_uso_cursor_ChaveNatural",
                table: "eventos_uso_cursor",
                column: "ChaveNatural",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_eventos_uso_cursor_Modelo_OcorridoEm",
                table: "eventos_uso_cursor",
                columns: new[] { "Modelo", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_uso_cursor_OcorridoEm",
                table: "eventos_uso_cursor",
                column: "OcorridoEm");

            migrationBuilder.CreateIndex(
                name: "IX_importacoes_uso_cursor_CriadoEm",
                table: "importacoes_uso_cursor",
                column: "CriadoEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_uso_cursor");

            migrationBuilder.DropTable(
                name: "importacoes_uso_cursor");
        }
    }
}
