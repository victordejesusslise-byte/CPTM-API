using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class ClasseDoPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A classe vem do pedido (Victor, 2026-10-16): o feixe ganha a classe, preenchida com a do item do pedido
            // (mesmo tamanho e espécie, ou sem espécie); sem item que sirva, fica a classe que a espécie tinha. Depois a espécie perde a classe.
            migrationBuilder.AddColumn<byte>(
                name: "classe",
                table: "FEIXE",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.Sql(@"
UPDATE f SET f.classe = COALESCE(
    (SELECT TOP 1 i.classe FROM PEDIDO_ITEM i JOIN RECEBIMENTO r ON r.id_pedido = i.id_pedido
     WHERE r.id_recebimento = f.id_recebimento AND i.id_tamanho = f.id_tamanho AND (i.id_especie = f.id_especie OR i.id_especie IS NULL)
     ORDER BY CASE WHEN i.id_especie = f.id_especie THEN 0 ELSE 1 END),
    e.classe)
FROM FEIXE f JOIN ESPECIE e ON e.id_especie = f.id_especie;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FEIXE_classe",
                table: "FEIXE",
                sql: "[classe] IN (1, 2)");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ESPECIE_classe",
                table: "ESPECIE");

            migrationBuilder.DropColumn(
                name: "classe",
                table: "ESPECIE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_FEIXE_classe",
                table: "FEIXE");

            migrationBuilder.DropColumn(
                name: "classe",
                table: "FEIXE");

            migrationBuilder.AddColumn<byte>(
                name: "classe",
                table: "ESPECIE",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 1,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 2,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 3,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 4,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 5,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 6,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 7,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 8,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 9,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 10,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 11,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 12,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 13,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 14,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 15,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 16,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 17,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 18,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 19,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 20,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 21,
                column: "classe",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 22,
                column: "classe",
                value: (byte)2);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ESPECIE_classe",
                table: "ESPECIE",
                sql: "[classe] IN (1, 2)");
        }
    }
}
