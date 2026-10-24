using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class TamanhoNoCodigoEPadraoDoFornecedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FEIXE_TAMANHO_DORMENTE_id_tamanho",
                table: "FEIXE");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTA_FISCAL_ITEM_TAMANHO_DORMENTE_id_tamanho",
                table: "NOTA_FISCAL_ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_OFERTA_FORNECEDOR_TAMANHO_DORMENTE_id_tamanho",
                table: "OFERTA_FORNECEDOR");

            migrationBuilder.DropForeignKey(
                name: "FK_PEDIDO_ITEM_TAMANHO_DORMENTE_id_tamanho",
                table: "PEDIDO_ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_PILHA_TAMANHO_DORMENTE_id_tamanho",
                table: "PILHA");

            // Os padrões de medida do fornecedor (Victor, 2026-10-23) nascem antes de apagar o tamanho: cada fornecedor (e quem já tem
            // pedido) ganha um padrão para todas as espécies, com a largura e a altura que estavam no tamanho (0,24 × 0,17).
            migrationBuilder.CreateTable(
                name: "MEDIDA_FORNECEDOR",
                columns: table => new
                {
                    id_medida = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_empresa = table.Column<int>(type: "int", nullable: false),
                    id_especie = table.Column<int>(type: "int", nullable: true),
                    largura_m = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false),
                    altura_m = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false),
                    ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEDIDA_FORNECEDOR", x => x.id_medida);
                    table.CheckConstraint("CK_MEDIDA_FORNECEDOR_medidas", "[largura_m] > 0 AND [altura_m] > 0");
                    table.ForeignKey(
                        name: "FK_MEDIDA_FORNECEDOR_EMPRESA_id_empresa",
                        column: x => x.id_empresa,
                        principalTable: "EMPRESA",
                        principalColumn: "id_empresa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MEDIDA_FORNECEDOR_ESPECIE_id_especie",
                        column: x => x.id_especie,
                        principalTable: "ESPECIE",
                        principalColumn: "id_especie",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                "INSERT INTO MEDIDA_FORNECEDOR (id_empresa, id_especie, largura_m, altura_m, ativo) " +
                "SELECT e.id_empresa, NULL, " +
                "ISNULL((SELECT TOP 1 largura_m FROM TAMANHO_DORMENTE ORDER BY id_tamanho), 0.24), " +
                "ISNULL((SELECT TOP 1 altura_m FROM TAMANHO_DORMENTE ORDER BY id_tamanho), 0.17), 1 " +
                "FROM EMPRESA e WHERE e.papel = 'Fornecedor' OR e.id_empresa IN (SELECT id_empresa FROM PEDIDO);");

            // Os códigos de material do ERP vão para PARAMETRO (erp_codigo_material_<comprimento>_<classe>).
            migrationBuilder.Sql(
                "INSERT INTO PARAMETRO (chave, valor) " +
                "SELECT CONCAT('erp_codigo_material_', id_tamanho, '_1'), codigo_erp_classe1 FROM TAMANHO_DORMENTE WHERE codigo_erp_classe1 IS NOT NULL " +
                "UNION ALL " +
                "SELECT CONCAT('erp_codigo_material_', id_tamanho, '_2'), codigo_erp_classe2 FROM TAMANHO_DORMENTE WHERE codigo_erp_classe2 IS NOT NULL;");

            migrationBuilder.DropTable(
                name: "TAMANHO_DORMENTE");

            migrationBuilder.DropIndex(
                name: "IX_PILHA_id_tamanho",
                table: "PILHA");

            migrationBuilder.DropIndex(
                name: "IX_PEDIDO_ITEM_id_tamanho",
                table: "PEDIDO_ITEM");

            migrationBuilder.DropIndex(
                name: "IX_OFERTA_FORNECEDOR_id_tamanho",
                table: "OFERTA_FORNECEDOR");

            migrationBuilder.DropIndex(
                name: "IX_NOTA_FISCAL_ITEM_id_tamanho",
                table: "NOTA_FISCAL_ITEM");

            migrationBuilder.DropIndex(
                name: "IX_FEIXE_id_tamanho",
                table: "FEIXE");

            // O item do pedido e o feixe ficam com o padrão do fornecedor do pedido (o único que ele tem até aqui).
            migrationBuilder.AddColumn<int>(
                name: "id_medida",
                table: "PEDIDO_ITEM",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_medida",
                table: "FEIXE",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE i SET i.id_medida = m.id_medida FROM PEDIDO_ITEM i " +
                "JOIN PEDIDO p ON p.id_pedido = i.id_pedido JOIN MEDIDA_FORNECEDOR m ON m.id_empresa = p.id_empresa;");
            migrationBuilder.Sql(
                "UPDATE f SET f.id_medida = m.id_medida FROM FEIXE f JOIN RECEBIMENTO r ON r.id_recebimento = f.id_recebimento " +
                "JOIN PEDIDO p ON p.id_pedido = r.id_pedido JOIN MEDIDA_FORNECEDOR m ON m.id_empresa = p.id_empresa;");

            migrationBuilder.AlterColumn<int>(
                name: "id_medida",
                table: "PEDIDO_ITEM",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id_medida",
                table: "FEIXE",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PILHA_id_tamanho",
                table: "PILHA",
                sql: "[id_tamanho] IN (1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_ITEM_id_medida",
                table: "PEDIDO_ITEM",
                column: "id_medida");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PEDIDO_ITEM_id_tamanho",
                table: "PEDIDO_ITEM",
                sql: "[id_tamanho] IN (1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OFERTA_FORNECEDOR_id_tamanho",
                table: "OFERTA_FORNECEDOR",
                sql: "[id_tamanho] IN (1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NOTA_FISCAL_ITEM_id_tamanho",
                table: "NOTA_FISCAL_ITEM",
                sql: "[id_tamanho] IN (1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_FEIXE_id_medida",
                table: "FEIXE",
                column: "id_medida");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FEIXE_id_tamanho",
                table: "FEIXE",
                sql: "[id_tamanho] IN (1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_MEDIDA_FORNECEDOR_id_empresa",
                table: "MEDIDA_FORNECEDOR",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_MEDIDA_FORNECEDOR_id_especie",
                table: "MEDIDA_FORNECEDOR",
                column: "id_especie");

            migrationBuilder.AddForeignKey(
                name: "FK_FEIXE_MEDIDA_FORNECEDOR_id_medida",
                table: "FEIXE",
                column: "id_medida",
                principalTable: "MEDIDA_FORNECEDOR",
                principalColumn: "id_medida",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PEDIDO_ITEM_MEDIDA_FORNECEDOR_id_medida",
                table: "PEDIDO_ITEM",
                column: "id_medida",
                principalTable: "MEDIDA_FORNECEDOR",
                principalColumn: "id_medida",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FEIXE_MEDIDA_FORNECEDOR_id_medida",
                table: "FEIXE");

            migrationBuilder.DropForeignKey(
                name: "FK_PEDIDO_ITEM_MEDIDA_FORNECEDOR_id_medida",
                table: "PEDIDO_ITEM");

            migrationBuilder.DropTable(
                name: "MEDIDA_FORNECEDOR");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PILHA_id_tamanho",
                table: "PILHA");

            migrationBuilder.DropIndex(
                name: "IX_PEDIDO_ITEM_id_medida",
                table: "PEDIDO_ITEM");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PEDIDO_ITEM_id_tamanho",
                table: "PEDIDO_ITEM");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OFERTA_FORNECEDOR_id_tamanho",
                table: "OFERTA_FORNECEDOR");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NOTA_FISCAL_ITEM_id_tamanho",
                table: "NOTA_FISCAL_ITEM");

            migrationBuilder.DropIndex(
                name: "IX_FEIXE_id_medida",
                table: "FEIXE");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FEIXE_id_tamanho",
                table: "FEIXE");

            migrationBuilder.DropColumn(
                name: "id_medida",
                table: "PEDIDO_ITEM");

            migrationBuilder.DropColumn(
                name: "id_medida",
                table: "FEIXE");

            migrationBuilder.CreateTable(
                name: "TAMANHO_DORMENTE",
                columns: table => new
                {
                    id_tamanho = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    altura_m = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false),
                    codigo_erp_classe1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    codigo_erp_classe2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    comprimento_m = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false),
                    largura_m = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: false),
                    medida_provisoria = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TAMANHO_DORMENTE", x => x.id_tamanho);
                });

            migrationBuilder.InsertData(
                table: "TAMANHO_DORMENTE",
                columns: new[] { "id_tamanho", "altura_m", "codigo_erp_classe1", "codigo_erp_classe2", "comprimento_m", "largura_m", "medida_provisoria" },
                values: new object[,]
                {
                    { 1, 0.17m, null, null, 2.80m, 0.24m, true },
                    { 2, 0.17m, null, null, 3.00m, 0.24m, true },
                    { 3, 0.17m, null, null, 4.00m, 0.24m, true },
                    { 4, 0.17m, null, null, 4.80m, 0.24m, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PILHA_id_tamanho",
                table: "PILHA",
                column: "id_tamanho");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_ITEM_id_tamanho",
                table: "PEDIDO_ITEM",
                column: "id_tamanho");

            migrationBuilder.CreateIndex(
                name: "IX_OFERTA_FORNECEDOR_id_tamanho",
                table: "OFERTA_FORNECEDOR",
                column: "id_tamanho");

            migrationBuilder.CreateIndex(
                name: "IX_NOTA_FISCAL_ITEM_id_tamanho",
                table: "NOTA_FISCAL_ITEM",
                column: "id_tamanho");

            migrationBuilder.CreateIndex(
                name: "IX_FEIXE_id_tamanho",
                table: "FEIXE",
                column: "id_tamanho");

            migrationBuilder.CreateIndex(
                name: "IX_TAMANHO_DORMENTE_comprimento_m",
                table: "TAMANHO_DORMENTE",
                column: "comprimento_m",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FEIXE_TAMANHO_DORMENTE_id_tamanho",
                table: "FEIXE",
                column: "id_tamanho",
                principalTable: "TAMANHO_DORMENTE",
                principalColumn: "id_tamanho",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTA_FISCAL_ITEM_TAMANHO_DORMENTE_id_tamanho",
                table: "NOTA_FISCAL_ITEM",
                column: "id_tamanho",
                principalTable: "TAMANHO_DORMENTE",
                principalColumn: "id_tamanho",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OFERTA_FORNECEDOR_TAMANHO_DORMENTE_id_tamanho",
                table: "OFERTA_FORNECEDOR",
                column: "id_tamanho",
                principalTable: "TAMANHO_DORMENTE",
                principalColumn: "id_tamanho",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PEDIDO_ITEM_TAMANHO_DORMENTE_id_tamanho",
                table: "PEDIDO_ITEM",
                column: "id_tamanho",
                principalTable: "TAMANHO_DORMENTE",
                principalColumn: "id_tamanho",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PILHA_TAMANHO_DORMENTE_id_tamanho",
                table: "PILHA",
                column: "id_tamanho",
                principalTable: "TAMANHO_DORMENTE",
                principalColumn: "id_tamanho",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
