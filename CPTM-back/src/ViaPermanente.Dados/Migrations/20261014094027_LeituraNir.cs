using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class LeituraNir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "confianca_pct",
                table: "CLASSIFICACAO_MADEIRA",
                type: "decimal(7,4)",
                precision: 7,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "umidade_pct",
                table: "CLASSIFICACAO_MADEIRA",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "confianca_pct",
                table: "CLASSIFICACAO_MADEIRA");

            migrationBuilder.DropColumn(
                name: "umidade_pct",
                table: "CLASSIFICACAO_MADEIRA");
        }
    }
}
