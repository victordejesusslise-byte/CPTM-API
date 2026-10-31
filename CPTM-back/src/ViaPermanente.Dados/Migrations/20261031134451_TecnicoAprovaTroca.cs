using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class TecnicoAprovaTroca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PARAMETRO",
                keyColumn: "chave",
                keyValue: "troca_quem_aprova",
                column: "valor",
                value: "supervisor,tecnico");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PARAMETRO",
                keyColumn: "chave",
                keyValue: "troca_quem_aprova",
                column: "valor",
                value: "supervisor");
        }
    }
}
