using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class SessaoETentativas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "esperar_ate",
                table: "USUARIO",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tentativas_falhas",
                table: "USUARIO",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "versao_sessao",
                table: "USUARIO",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "esperar_ate",
                table: "USUARIO");

            migrationBuilder.DropColumn(
                name: "tentativas_falhas",
                table: "USUARIO");

            migrationBuilder.DropColumn(
                name: "versao_sessao",
                table: "USUARIO");
        }
    }
}
