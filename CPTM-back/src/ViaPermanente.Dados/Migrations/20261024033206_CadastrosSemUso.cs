using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class CadastrosSemUso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_nascimento",
                table: "USUARIO");

            migrationBuilder.DropColumn(
                name: "descricao",
                table: "SETOR");

            migrationBuilder.DropColumn(
                name: "endereco",
                table: "EMPRESA");

            migrationBuilder.DropColumn(
                name: "telefone",
                table: "EMPRESA");

            migrationBuilder.DropColumn(
                name: "frente",
                table: "DORMENTE");

            migrationBuilder.DropColumn(
                name: "endereco",
                table: "ARMAZEM");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "data_nascimento",
                table: "USUARIO",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "descricao",
                table: "SETOR",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco",
                table: "EMPRESA",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telefone",
                table: "EMPRESA",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "frente",
                table: "DORMENTE",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco",
                table: "ARMAZEM",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ARMAZEM",
                keyColumn: "id_armazem",
                keyValue: 1,
                column: "endereco",
                value: null);

            migrationBuilder.UpdateData(
                table: "ARMAZEM",
                keyColumn: "id_armazem",
                keyValue: 2,
                column: "endereco",
                value: null);

            migrationBuilder.UpdateData(
                table: "ARMAZEM",
                keyColumn: "id_armazem",
                keyValue: 3,
                column: "endereco",
                value: null);

            migrationBuilder.UpdateData(
                table: "ARMAZEM",
                keyColumn: "id_armazem",
                keyValue: 4,
                column: "endereco",
                value: null);

            migrationBuilder.UpdateData(
                table: "ARMAZEM",
                keyColumn: "id_armazem",
                keyValue: 5,
                column: "endereco",
                value: null);
        }
    }
}
