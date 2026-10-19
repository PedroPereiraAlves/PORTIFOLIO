using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedroHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoramento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alvos_monitorados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Metodo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IntervaloSegundos = table.Column<int>(type: "integer", nullable: false),
                    TimeoutMs = table.Column<int>(type: "integer", nullable: false),
                    StatusEsperado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LimiarDegradadoMs = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ProjetoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Infra = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StatusAtual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FalhasConsecutivas = table.Column<int>(type: "integer", nullable: false),
                    UltimaChecagemEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProximaChecagemEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaMudancaStatusEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alvos_monitorados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alvos_monitorados_projetos_ProjetoId",
                        column: x => x.ProjetoId,
                        principalTable: "projetos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "checagens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlvoMonitoradoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LatenciaMs = table.Column<int>(type: "integer", nullable: false),
                    CodigoHttp = table.Column<int>(type: "integer", nullable: true),
                    Erro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checagens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_checagens_alvos_monitorados_AlvoMonitoradoId",
                        column: x => x.AlvoMonitoradoId,
                        principalTable: "alvos_monitorados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alvos_monitorados_Ativo_ProximaChecagemEm",
                table: "alvos_monitorados",
                columns: new[] { "Ativo", "ProximaChecagemEm" });

            migrationBuilder.CreateIndex(
                name: "IX_alvos_monitorados_ProjetoId",
                table: "alvos_monitorados",
                column: "ProjetoId");

            migrationBuilder.CreateIndex(
                name: "IX_alvos_monitorados_ProximaChecagemEm",
                table: "alvos_monitorados",
                column: "ProximaChecagemEm");

            migrationBuilder.CreateIndex(
                name: "IX_alvos_monitorados_Url",
                table: "alvos_monitorados",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_checagens_AlvoMonitoradoId_ExecutadaEm",
                table: "checagens",
                columns: new[] { "AlvoMonitoradoId", "ExecutadaEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checagens");

            migrationBuilder.DropTable(
                name: "alvos_monitorados");
        }
    }
}
