using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class ParametroToleranciaNota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PARAMETRO",
                columns: new[] { "chave", "alterado_em", "alterado_por", "valor" },
                values: new object[] { "nota_tolerancia_volume_pct", null, null, "2" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PARAMETRO",
                keyColumn: "chave",
                keyValue: "nota_tolerancia_volume_pct");
        }
    }
}
