using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class TipoLeitorNoCodigo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LEITOR_TIPO_LEITOR_id_tipo_leitor",
                table: "LEITOR");

            migrationBuilder.DropTable(
                name: "TIPO_LEITOR");

            migrationBuilder.DropIndex(
                name: "IX_LEITOR_id_tipo_leitor",
                table: "LEITOR");

            migrationBuilder.RenameColumn(
                name: "id_tipo_leitor",
                table: "LEITOR",
                newName: "tipo");

            // Os números mudam com a lista do código (Victor, 2026-10-23): o módulo passa a ser o 1 e a pistola o 2 (era o contrário
            // na tabela TIPO_LEITOR); o carro continua 3.
            migrationBuilder.Sql("UPDATE LEITOR SET tipo = CASE tipo WHEN 1 THEN 2 WHEN 2 THEN 1 ELSE tipo END;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LEITOR_tipo",
                table: "LEITOR",
                sql: "[tipo] IN (1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LEITOR_tipo",
                table: "LEITOR");

            migrationBuilder.Sql("UPDATE LEITOR SET tipo = CASE tipo WHEN 1 THEN 2 WHEN 2 THEN 1 ELSE tipo END;");

            migrationBuilder.RenameColumn(
                name: "tipo",
                table: "LEITOR",
                newName: "id_tipo_leitor");

            migrationBuilder.CreateTable(
                name: "TIPO_LEITOR",
                columns: table => new
                {
                    id_tipo_leitor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIPO_LEITOR", x => x.id_tipo_leitor);
                });

            migrationBuilder.InsertData(
                table: "TIPO_LEITOR",
                columns: new[] { "id_tipo_leitor", "descricao", "nome" },
                values: new object[,]
                {
                    { 1, "Analisa a madeira por infravermelho próximo: espécie, umidade e propriedades", "Pistola NIR" },
                    { 2, "Módulo RFID UHF ligado pelo USB-C", "Módulo do celular" },
                    { 3, "Carro de inspeção da varredura noturna", "Carro" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LEITOR_id_tipo_leitor",
                table: "LEITOR",
                column: "id_tipo_leitor");

            migrationBuilder.CreateIndex(
                name: "IX_TIPO_LEITOR_nome",
                table: "TIPO_LEITOR",
                column: "nome",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LEITOR_TIPO_LEITOR_id_tipo_leitor",
                table: "LEITOR",
                column: "id_tipo_leitor",
                principalTable: "TIPO_LEITOR",
                principalColumn: "id_tipo_leitor",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
