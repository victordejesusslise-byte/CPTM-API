using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class ParametrosDesvio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PARAMETRO",
                columns: new[] { "chave", "alterado_em", "alterado_por", "valor" },
                values: new object[,]
                {
                    { "desvio_atencao_mm", null, null, "20" },
                    { "desvio_limite_mm", null, null, "40" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PARAMETRO",
                keyColumn: "chave",
                keyValue: "desvio_atencao_mm");

            migrationBuilder.DeleteData(
                table: "PARAMETRO",
                keyColumn: "chave",
                keyValue: "desvio_limite_mm");
        }
    }
}
