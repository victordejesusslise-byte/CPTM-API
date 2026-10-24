using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class PessoaNoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PESSOA entra no USUARIO (Victor, 2026-10-23): primeiro as colunas novas, depois a cópia dos dados, e só então sai a PESSOA.
            migrationBuilder.AddColumn<DateOnly>(
                name: "data_nascimento",
                table: "USUARIO",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "USUARIO",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "nome",
                table: "USUARIO",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE u SET u.nome = p.nome, u.email = ISNULL(p.email, CONCAT('usuario', u.id_usuario, '@sem-email.local')), u.data_nascimento = p.data_nascimento " +
                "FROM USUARIO u JOIN PESSOA p ON p.id_pessoa = u.id_pessoa;");

            migrationBuilder.DropForeignKey(
                name: "FK_USUARIO_PESSOA_id_pessoa",
                table: "USUARIO");

            migrationBuilder.DropIndex(
                name: "IX_USUARIO_id_pessoa",
                table: "USUARIO");

            migrationBuilder.DropColumn(
                name: "id_pessoa",
                table: "USUARIO");

            migrationBuilder.DropTable(
                name: "PESSOA");

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_email",
                table: "USUARIO",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_USUARIO_email",
                table: "USUARIO");

            migrationBuilder.DropColumn(
                name: "data_nascimento",
                table: "USUARIO");

            migrationBuilder.DropColumn(
                name: "email",
                table: "USUARIO");

            migrationBuilder.DropColumn(
                name: "nome",
                table: "USUARIO");

            migrationBuilder.AddColumn<int>(
                name: "id_pessoa",
                table: "USUARIO",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PESSOA",
                columns: table => new
                {
                    id_pessoa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    data_nascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PESSOA", x => x.id_pessoa);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_id_pessoa",
                table: "USUARIO",
                column: "id_pessoa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PESSOA_email",
                table: "PESSOA",
                column: "email",
                unique: true,
                filter: "[email] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_USUARIO_PESSOA_id_pessoa",
                table: "USUARIO",
                column: "id_pessoa",
                principalTable: "PESSOA",
                principalColumn: "id_pessoa",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
