using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class SiglaDaEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sigla",
                table: "EMPRESA",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true);

            // As empresas que já existem ganham o que vem antes do primeiro traço do nome ("TG-Três Guri" → TG), se for de 2 a 6 letras ou
            // números. O travessão da placa ("TG–Três Guri", "TG—Três Guri") também conta como traço. As outras ficam sem sigla.
            migrationBuilder.Sql(
                """
                UPDATE e SET sigla = UPPER(x.antes)
                FROM EMPRESA e
                CROSS APPLY (SELECT PATINDEX(N'%[-–—]%', e.nome COLLATE Latin1_General_BIN) AS traco) t
                CROSS APPLY (SELECT LTRIM(RTRIM(LEFT(e.nome, CASE WHEN t.traco > 1 THEN t.traco - 1 ELSE 0 END))) AS antes) x
                WHERE t.traco > 1
                  AND LEN(x.antes) BETWEEN 2 AND 6
                  AND x.antes NOT LIKE N'%[^A-Za-z0-9]%' COLLATE Latin1_General_BIN;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_EMPRESA_sigla",
                table: "EMPRESA",
                sql: "[sigla] IS NULL OR (LEN([sigla]) BETWEEN 2 AND 6 AND [sigla] NOT LIKE '%[^A-Z0-9]%' COLLATE Latin1_General_BIN)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_EMPRESA_sigla",
                table: "EMPRESA");

            migrationBuilder.DropColumn(
                name: "sigla",
                table: "EMPRESA");
        }
    }
}
