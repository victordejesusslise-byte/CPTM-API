using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class TrocaSemRede : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "id_cliente",
                table: "TROCA",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "recebido_em",
                table: "TROCA",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_TROCA_id_cliente",
                table: "TROCA",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TROCA_id_cliente",
                table: "TROCA");

            migrationBuilder.DropColumn(
                name: "id_cliente",
                table: "TROCA");

            migrationBuilder.DropColumn(
                name: "recebido_em",
                table: "TROCA");
        }
    }
}
