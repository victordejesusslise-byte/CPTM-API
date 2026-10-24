using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class CodigoErpNoTamanho : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ativo",
                table: "TAMANHO_DORMENTE");

            migrationBuilder.AddColumn<string>(
                name: "codigo_erp_classe1",
                table: "TAMANHO_DORMENTE",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_erp_classe2",
                table: "TAMANHO_DORMENTE",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // O código do ERP vai para o tamanho (Victor, 2026-10-23): copia do CODIGO_MATERIAL antes de apagar a tabela.
            migrationBuilder.Sql(
                "UPDATE t SET t.codigo_erp_classe1 = (SELECT TOP 1 c.codigo_erp FROM CODIGO_MATERIAL c WHERE c.id_tamanho = t.id_tamanho AND c.classe = 1), " +
                "t.codigo_erp_classe2 = (SELECT TOP 1 c.codigo_erp FROM CODIGO_MATERIAL c WHERE c.id_tamanho = t.id_tamanho AND c.classe = 2) " +
                "FROM TAMANHO_DORMENTE t;");

            migrationBuilder.DropTable(
                name: "CODIGO_MATERIAL");

            // (O gerador do EF punha aqui 4 UpdateData que regravavam os códigos dos tamanhos como nulos, apagando a cópia acima: saíram.)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "codigo_erp_classe1",
                table: "TAMANHO_DORMENTE");

            migrationBuilder.DropColumn(
                name: "codigo_erp_classe2",
                table: "TAMANHO_DORMENTE");

            migrationBuilder.AddColumn<bool>(
                name: "ativo",
                table: "TAMANHO_DORMENTE",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CODIGO_MATERIAL",
                columns: table => new
                {
                    id_codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    classe = table.Column<byte>(type: "tinyint", nullable: false),
                    codigo_erp = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    id_tamanho = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CODIGO_MATERIAL", x => x.id_codigo);
                    table.CheckConstraint("CK_CODIGO_MATERIAL_classe", "[classe] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_CODIGO_MATERIAL_TAMANHO_DORMENTE_id_tamanho",
                        column: x => x.id_tamanho,
                        principalTable: "TAMANHO_DORMENTE",
                        principalColumn: "id_tamanho",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "TAMANHO_DORMENTE",
                keyColumn: "id_tamanho",
                keyValue: 1,
                column: "ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "TAMANHO_DORMENTE",
                keyColumn: "id_tamanho",
                keyValue: 2,
                column: "ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "TAMANHO_DORMENTE",
                keyColumn: "id_tamanho",
                keyValue: 3,
                column: "ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "TAMANHO_DORMENTE",
                keyColumn: "id_tamanho",
                keyValue: 4,
                column: "ativo",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_CODIGO_MATERIAL_id_tamanho_classe",
                table: "CODIGO_MATERIAL",
                columns: new[] { "id_tamanho", "classe" },
                unique: true);
        }
    }
}
