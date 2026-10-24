using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class PlanoDaNoite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "noite_do_plano",
                table: "TROCA",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "urgente",
                table: "TROCA",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "PARAMETRO",
                columns: new[] { "chave", "alterado_em", "alterado_por", "valor" },
                values: new object[] { "troca_plano_horario", null, null, "20:00" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PARAMETRO",
                keyColumn: "chave",
                keyValue: "troca_plano_horario");

            migrationBuilder.DropColumn(
                name: "noite_do_plano",
                table: "TROCA");

            migrationBuilder.DropColumn(
                name: "urgente",
                table: "TROCA");
        }
    }
}
