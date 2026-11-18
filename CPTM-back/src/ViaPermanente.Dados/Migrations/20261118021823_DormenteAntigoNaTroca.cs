using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <summary>
    /// Dormente que nunca teve tag (Victor, 2026-11-17): a TROCA ganha saiu_nunca_teve_tag, e a peça que sai (id_dormente_saiu) pode ficar
    /// vazia, para o dormente antigo, sem cadastro, que sai num lote só pelo km. As trocas de hoje não mudam.
    /// </summary>
    public partial class DormenteAntigoNaTroca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "id_dormente_saiu",
                table: "TROCA",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "saiu_nunca_teve_tag",
                table: "TROCA",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "saiu_nunca_teve_tag",
                table: "TROCA");

            migrationBuilder.AlterColumn<int>(
                name: "id_dormente_saiu",
                table: "TROCA",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
