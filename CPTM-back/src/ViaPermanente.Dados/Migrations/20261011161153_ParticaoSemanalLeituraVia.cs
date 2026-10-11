using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <summary>
    /// LEITURA_VIA em partições de uma semana (segunda a domingo), pela coluna data, para descartar o que passa de 90 dias
    /// semana a semana (TRUNCATE da partição), sem varrer a tabela. As semanas seguintes são abertas pela manutenção diária.
    /// </summary>
    public partial class ParticaoSemanalLeituraVia : Migration
    {
        private const string Funcao = "PF_LEITURA_VIA_SEMANA";
        private const string Esquema = "PS_LEITURA_VIA_SEMANA";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Segundas-feiras de 29/06/2026 a 28/12/2026; a manutenção acrescenta as seguintes e junta as velhas.
            var segundas = Enumerable.Range(0, 27).Select(i => new DateTime(2026, 6, 29).AddDays(7 * i).ToString("yyyy-MM-dd"));
            migrationBuilder.Sql($"CREATE PARTITION FUNCTION {Funcao} (datetime2(7)) AS RANGE RIGHT FOR VALUES ({string.Join(", ", segundas.Select(s => $"'{s}'"))});");
            migrationBuilder.Sql($"CREATE PARTITION SCHEME {Esquema} AS PARTITION {Funcao} ALL TO ([PRIMARY]);");
            migrationBuilder.Sql("DROP INDEX IX_LEITURA_VIA_id_dormente_data ON LEITURA_VIA;");
            migrationBuilder.Sql("DROP INDEX IX_LEITURA_VIA_id_passagem ON LEITURA_VIA;");
            migrationBuilder.Sql("ALTER TABLE LEITURA_VIA DROP CONSTRAINT PK_LEITURA_VIA;");
            migrationBuilder.Sql($"ALTER TABLE LEITURA_VIA ADD CONSTRAINT PK_LEITURA_VIA PRIMARY KEY CLUSTERED (id_leitura_via, data) ON {Esquema}(data);");
            migrationBuilder.Sql($"CREATE INDEX IX_LEITURA_VIA_id_dormente_data ON LEITURA_VIA (id_dormente, data) ON {Esquema}(data);");
            migrationBuilder.Sql($"CREATE INDEX IX_LEITURA_VIA_id_passagem ON LEITURA_VIA (id_passagem) ON {Esquema}(data);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IX_LEITURA_VIA_id_dormente_data ON LEITURA_VIA;");
            migrationBuilder.Sql("DROP INDEX IX_LEITURA_VIA_id_passagem ON LEITURA_VIA;");
            migrationBuilder.Sql("ALTER TABLE LEITURA_VIA DROP CONSTRAINT PK_LEITURA_VIA;");
            migrationBuilder.Sql("ALTER TABLE LEITURA_VIA ADD CONSTRAINT PK_LEITURA_VIA PRIMARY KEY CLUSTERED (id_leitura_via, data) ON [PRIMARY];");
            migrationBuilder.Sql("CREATE INDEX IX_LEITURA_VIA_id_dormente_data ON LEITURA_VIA (id_dormente, data);");
            migrationBuilder.Sql("CREATE INDEX IX_LEITURA_VIA_id_passagem ON LEITURA_VIA (id_passagem);");
            migrationBuilder.Sql($"DROP PARTITION SCHEME {Esquema};");
            migrationBuilder.Sql($"DROP PARTITION FUNCTION {Funcao};");
        }
    }
}