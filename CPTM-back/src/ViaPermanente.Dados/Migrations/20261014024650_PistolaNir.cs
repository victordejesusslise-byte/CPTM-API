using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class PistolaNir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TIPO_LEITOR",
                keyColumn: "id_tipo_leitor",
                keyValue: 1,
                columns: new[] { "descricao", "nome" },
                values: new object[] { "Analisa a madeira por infravermelho próximo: espécie, umidade e propriedades", "Pistola NIR" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TIPO_LEITOR",
                keyColumn: "id_tipo_leitor",
                keyValue: 1,
                columns: new[] { "descricao", "nome" },
                values: new object[] { "Crava a tag e manda o que os sensores mediram", "Pistola" });
        }
    }
}
