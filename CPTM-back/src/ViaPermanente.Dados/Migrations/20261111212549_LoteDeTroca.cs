using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class LoteDeTroca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_lote_troca",
                table: "TROCA",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id_troca",
                table: "RECUSA_TROCA",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "id_lote_troca",
                table: "RECUSA_TROCA",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_lote_troca",
                table: "MOVIMENTACAO",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LOTE_TROCA",
                columns: table => new
                {
                    id_lote_troca = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_trecho = table.Column<int>(type: "int", nullable: false),
                    km_inicio = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    km_fim = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    id_tamanho = table.Column<int>(type: "int", nullable: false),
                    quantidade_pedida = table.Column<int>(type: "int", nullable: false),
                    quantidade_aprovada = table.Column<int>(type: "int", nullable: true),
                    motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    id_usuario_pediu = table.Column<int>(type: "int", nullable: false),
                    data_pedido = table.Column<DateTime>(type: "datetime2", nullable: false),
                    id_usuario_aprovou = table.Column<int>(type: "int", nullable: true),
                    data_aprovacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    motivo_reprovacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    motivo_menos = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    noite_do_plano = table.Column<DateOnly>(type: "date", nullable: true),
                    urgente = table.Column<bool>(type: "bit", nullable: false),
                    id_usuario_fez = table.Column<int>(type: "int", nullable: true),
                    data = table.Column<DateTime>(type: "datetime2", nullable: true),
                    equipe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    id_cliente = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recebido_em = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cancelamento = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOTE_TROCA", x => x.id_lote_troca);
                    table.CheckConstraint("CK_LOTE_TROCA_id_tamanho", "[id_tamanho] IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_LOTE_TROCA_TRECHO_id_trecho",
                        column: x => x.id_trecho,
                        principalTable: "TRECHO",
                        principalColumn: "id_trecho",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LOTE_TROCA_USUARIO_id_usuario_aprovou",
                        column: x => x.id_usuario_aprovou,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LOTE_TROCA_USUARIO_id_usuario_fez",
                        column: x => x.id_usuario_fez,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LOTE_TROCA_USUARIO_id_usuario_pediu",
                        column: x => x.id_usuario_pediu,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TROCA_id_lote_troca",
                table: "TROCA",
                column: "id_lote_troca");

            migrationBuilder.CreateIndex(
                name: "IX_RECUSA_TROCA_id_lote_troca",
                table: "RECUSA_TROCA",
                column: "id_lote_troca");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RECUSA_TROCA_troca_ou_lote",
                table: "RECUSA_TROCA",
                sql: "([id_troca] IS NOT NULL AND [id_lote_troca] IS NULL) OR ([id_troca] IS NULL AND [id_lote_troca] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_MOVIMENTACAO_id_lote_troca",
                table: "MOVIMENTACAO",
                column: "id_lote_troca");

            migrationBuilder.CreateIndex(
                name: "IX_LOTE_TROCA_id_trecho",
                table: "LOTE_TROCA",
                column: "id_trecho");

            migrationBuilder.CreateIndex(
                name: "IX_LOTE_TROCA_id_usuario_aprovou",
                table: "LOTE_TROCA",
                column: "id_usuario_aprovou");

            migrationBuilder.CreateIndex(
                name: "IX_LOTE_TROCA_id_usuario_fez",
                table: "LOTE_TROCA",
                column: "id_usuario_fez");

            migrationBuilder.CreateIndex(
                name: "IX_LOTE_TROCA_id_usuario_pediu",
                table: "LOTE_TROCA",
                column: "id_usuario_pediu");

            migrationBuilder.CreateIndex(
                name: "UX_LOTE_TROCA_id_cliente",
                table: "LOTE_TROCA",
                column: "id_cliente",
                unique: true,
                filter: "[id_cliente] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_MOVIMENTACAO_LOTE_TROCA_id_lote_troca",
                table: "MOVIMENTACAO",
                column: "id_lote_troca",
                principalTable: "LOTE_TROCA",
                principalColumn: "id_lote_troca",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RECUSA_TROCA_LOTE_TROCA_id_lote_troca",
                table: "RECUSA_TROCA",
                column: "id_lote_troca",
                principalTable: "LOTE_TROCA",
                principalColumn: "id_lote_troca",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TROCA_LOTE_TROCA_id_lote_troca",
                table: "TROCA",
                column: "id_lote_troca",
                principalTable: "LOTE_TROCA",
                principalColumn: "id_lote_troca",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MOVIMENTACAO_LOTE_TROCA_id_lote_troca",
                table: "MOVIMENTACAO");

            migrationBuilder.DropForeignKey(
                name: "FK_RECUSA_TROCA_LOTE_TROCA_id_lote_troca",
                table: "RECUSA_TROCA");

            migrationBuilder.DropForeignKey(
                name: "FK_TROCA_LOTE_TROCA_id_lote_troca",
                table: "TROCA");

            migrationBuilder.DropTable(
                name: "LOTE_TROCA");

            migrationBuilder.DropIndex(
                name: "IX_TROCA_id_lote_troca",
                table: "TROCA");

            migrationBuilder.DropIndex(
                name: "IX_RECUSA_TROCA_id_lote_troca",
                table: "RECUSA_TROCA");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RECUSA_TROCA_troca_ou_lote",
                table: "RECUSA_TROCA");

            migrationBuilder.DropIndex(
                name: "IX_MOVIMENTACAO_id_lote_troca",
                table: "MOVIMENTACAO");

            migrationBuilder.DropColumn(
                name: "id_lote_troca",
                table: "TROCA");

            migrationBuilder.DropColumn(
                name: "id_lote_troca",
                table: "RECUSA_TROCA");

            migrationBuilder.DropColumn(
                name: "id_lote_troca",
                table: "MOVIMENTACAO");

            migrationBuilder.AlterColumn<int>(
                name: "id_troca",
                table: "RECUSA_TROCA",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
