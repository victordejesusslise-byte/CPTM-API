using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class ModeloOcrInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MODELO_OCR",
                columns: new[] { "id_modelo", "acerto_teste", "amostras", "ativo", "id_usuario_ativou", "modelo_base", "nome", "treinado_em", "versao" },
                values: new object[] { 1, null, 0, true, null, "por", "placa-feixe", null, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MODELO_OCR",
                keyColumn: "id_modelo",
                keyValue: 1);
        }
    }
}
