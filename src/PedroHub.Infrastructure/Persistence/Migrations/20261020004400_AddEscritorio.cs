using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedroHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscritorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agentes_escritorio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Papel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Sala = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    CorAvatar = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SpriteKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agentes_escritorio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "eventos_agente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgenteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Link = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OcorridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_agente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eventos_agente_agentes_escritorio_AgenteId",
                        column: x => x.AgenteId,
                        principalTable: "agentes_escritorio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "status_agente_atual",
                columns: table => new
                {
                    AgenteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TarefaAtual = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    Projeto = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    PrUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AtualizadoEmPublicado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_agente_atual", x => x.AgenteId);
                    table.ForeignKey(
                        name: "FK_status_agente_atual_agentes_escritorio_AgenteId",
                        column: x => x.AgenteId,
                        principalTable: "agentes_escritorio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "status_agente_historico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgenteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TarefaAtual = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    Projeto = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    PrUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PublicadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_agente_historico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_status_agente_historico_agentes_escritorio_AgenteId",
                        column: x => x.AgenteId,
                        principalTable: "agentes_escritorio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agentes_escritorio_Sala_Ordem",
                table: "agentes_escritorio",
                columns: new[] { "Sala", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_agentes_escritorio_Slug",
                table: "agentes_escritorio",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_eventos_agente_AgenteId_OcorridoEm",
                table: "eventos_agente",
                columns: new[] { "AgenteId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_status_agente_historico_AgenteId_PublicadoEm",
                table: "status_agente_historico",
                columns: new[] { "AgenteId", "PublicadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_agente");

            migrationBuilder.DropTable(
                name: "status_agente_atual");

            migrationBuilder.DropTable(
                name: "status_agente_historico");

            migrationBuilder.DropTable(
                name: "agentes_escritorio");
        }
    }
}
