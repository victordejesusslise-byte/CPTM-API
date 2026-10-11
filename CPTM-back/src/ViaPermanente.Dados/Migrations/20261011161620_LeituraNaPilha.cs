using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class LeituraNaPilha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_pilha",
                table: "LEITURA_DORMENTE",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_DORMENTE_id_pilha",
                table: "LEITURA_DORMENTE",
                column: "id_pilha");

            migrationBuilder.AddForeignKey(
                name: "FK_LEITURA_DORMENTE_PILHA_id_pilha",
                table: "LEITURA_DORMENTE",
                column: "id_pilha",
                principalTable: "PILHA",
                principalColumn: "id_pilha",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LEITURA_DORMENTE_PILHA_id_pilha",
                table: "LEITURA_DORMENTE");

            migrationBuilder.DropIndex(
                name: "IX_LEITURA_DORMENTE_id_pilha",
                table: "LEITURA_DORMENTE");

            migrationBuilder.DropColumn(
                name: "id_pilha",
                table: "LEITURA_DORMENTE");
        }
    }
}
