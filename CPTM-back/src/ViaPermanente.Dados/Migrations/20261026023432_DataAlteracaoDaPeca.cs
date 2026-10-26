using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class DataAlteracaoDaPeca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "data_alteracao",
                table: "DORMENTE",
                type: "datetime2",
                nullable: true);

            // As peças que já existem entram na cópia das linhas como alteradas agora (o celular baixa tudo na primeira vez).
            migrationBuilder.Sql("UPDATE DORMENTE SET data_alteracao = SYSDATETIME();");

            migrationBuilder.CreateIndex(
                name: "IX_DORMENTE_data_alteracao",
                table: "DORMENTE",
                column: "data_alteracao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DORMENTE_data_alteracao",
                table: "DORMENTE");

            migrationBuilder.DropColumn(
                name: "data_alteracao",
                table: "DORMENTE");
        }
    }
}
