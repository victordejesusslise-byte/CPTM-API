using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class LeituraOcrDoPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_pedido",
                table: "LEITURA_OCR",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_OCR_id_pedido",
                table: "LEITURA_OCR",
                column: "id_pedido");

            migrationBuilder.AddForeignKey(
                name: "FK_LEITURA_OCR_PEDIDO_id_pedido",
                table: "LEITURA_OCR",
                column: "id_pedido",
                principalTable: "PEDIDO",
                principalColumn: "id_pedido",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LEITURA_OCR_PEDIDO_id_pedido",
                table: "LEITURA_OCR");

            migrationBuilder.DropIndex(
                name: "IX_LEITURA_OCR_id_pedido",
                table: "LEITURA_OCR");

            migrationBuilder.DropColumn(
                name: "id_pedido",
                table: "LEITURA_OCR");
        }
    }
}
