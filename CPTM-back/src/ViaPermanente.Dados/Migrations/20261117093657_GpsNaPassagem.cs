using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <summary>
    /// GPS na passagem (Victor, 2026-11-17): a passagem aponta a estação A e a B; a leitura e a detecção ganham a latitude e a longitude
    /// do GPS do veículo; e a PONTO_PASSAGEM guarda o caminho (um ponto por foto) com o km calculado. Só acrescenta, não apaga nada.
    /// </summary>
    public partial class GpsNaPassagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_estacao_fim",
                table: "PASSAGEM",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_estacao_inicio",
                table: "PASSAGEM",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "LEITURA_VIA",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "LEITURA_VIA",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "DETECCAO_IMAGEM",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "DETECCAO_IMAGEM",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PONTO_PASSAGEM",
                columns: table => new
                {
                    id_ponto = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_passagem = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    km_calculado = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PONTO_PASSAGEM", x => x.id_ponto);
                    table.ForeignKey(
                        name: "FK_PONTO_PASSAGEM_PASSAGEM_id_passagem",
                        column: x => x.id_passagem,
                        principalTable: "PASSAGEM",
                        principalColumn: "id_passagem",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PASSAGEM_id_estacao_fim",
                table: "PASSAGEM",
                column: "id_estacao_fim");

            migrationBuilder.CreateIndex(
                name: "IX_PASSAGEM_id_estacao_inicio",
                table: "PASSAGEM",
                column: "id_estacao_inicio");

            migrationBuilder.CreateIndex(
                name: "IX_PONTO_PASSAGEM_id_passagem_data",
                table: "PONTO_PASSAGEM",
                columns: new[] { "id_passagem", "data" });

            migrationBuilder.AddForeignKey(
                name: "FK_PASSAGEM_ESTACAO_id_estacao_fim",
                table: "PASSAGEM",
                column: "id_estacao_fim",
                principalTable: "ESTACAO",
                principalColumn: "id_estacao",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PASSAGEM_ESTACAO_id_estacao_inicio",
                table: "PASSAGEM",
                column: "id_estacao_inicio",
                principalTable: "ESTACAO",
                principalColumn: "id_estacao",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PASSAGEM_ESTACAO_id_estacao_fim",
                table: "PASSAGEM");

            migrationBuilder.DropForeignKey(
                name: "FK_PASSAGEM_ESTACAO_id_estacao_inicio",
                table: "PASSAGEM");

            migrationBuilder.DropTable(
                name: "PONTO_PASSAGEM");

            migrationBuilder.DropIndex(
                name: "IX_PASSAGEM_id_estacao_fim",
                table: "PASSAGEM");

            migrationBuilder.DropIndex(
                name: "IX_PASSAGEM_id_estacao_inicio",
                table: "PASSAGEM");

            migrationBuilder.DropColumn(
                name: "id_estacao_fim",
                table: "PASSAGEM");

            migrationBuilder.DropColumn(
                name: "id_estacao_inicio",
                table: "PASSAGEM");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "LEITURA_VIA");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "LEITURA_VIA");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "DETECCAO_IMAGEM");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "DETECCAO_IMAGEM");
        }
    }
}
