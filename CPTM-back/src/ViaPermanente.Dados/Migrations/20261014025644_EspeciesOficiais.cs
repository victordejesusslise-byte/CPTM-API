using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class EspeciesOficiais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                columns: new[] { "classe", "nome_cientifico" },
                values: new object[] { (byte)2, "Tabebuia serratifolia" });

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
                columns: new[] { "classe", "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { (byte)2, "Fabaceae", "Apuleia molaris", "Amarelão" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 6,
                columns: new[] { "classe", "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { (byte)2, "Apocynaceae", "Aspidosperma polyneuron", "Peroba-rosa" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 7,
                columns: new[] { "classe", "nome_cientifico", "nome_popular" },
                values: new object[] { (byte)2, "Astronium graveolens", "Guaritá" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 8,
                columns: new[] { "classe", "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { (byte)2, "Fabaceae", "Bowdichia brasiliensis ou Bowdichia spp", "Sucupira" });

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
                columns: new[] { "classe", "nome_cientifico" },
                values: new object[] { (byte)2, "Angelim Vermelho (Promap - AMV)" });

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
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Fabaceae", "Bowdichia racemosa", "Sucupira" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 14,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Fabaceae", "Dialium guianense", "Jutaí-pororoca" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 15,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Fabaceae", "Diplotropis purpurea", "Sucupira-preta" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 16,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Fabaceae", "Dipteryx spp.", "Cumaru" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 17,
                columns: new[] { "nome_cientifico", "nome_popular" },
                values: new object[] { "Enterolobium schomburgkii", "Orelha-de-macaco" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 18,
                column: "nome_cientifico",
                value: "Hymenolobium petraeum ou Hymenolobium spp");

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 19,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Fabaceae", "Hymenaea intermedia", "Jatobá" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 20,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "A confirmar", "Coimal (Dormentes antigos - AMV)", null });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 22,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "A confirmar", "Veneer (Dormentes antigos - AMV)", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 1,
                column: "classe",
                value: (byte)1);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 2,
                columns: new[] { "classe", "nome_cientifico" },
                values: new object[] { (byte)1, "Handroanthus serratifolius" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 3,
                column: "classe",
                value: (byte)1);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 4,
                column: "classe",
                value: (byte)1);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 5,
                columns: new[] { "classe", "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { (byte)1, "Sapotaceae", "Manilkara huberi", "Maçaranduba" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 6,
                columns: new[] { "classe", "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { (byte)1, "Anacardiaceae", "Myracrodruon urundeuva", "Aroeira" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 7,
                columns: new[] { "classe", "nome_cientifico", "nome_popular" },
                values: new object[] { (byte)1, "Astronium lecointei", "Muiracatiara" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 8,
                columns: new[] { "classe", "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { (byte)1, "Bignoniaceae", "Handroanthus impetiginosus", "Ipê-roxo" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 9,
                column: "classe",
                value: (byte)1);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 10,
                column: "classe",
                value: (byte)1);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 11,
                columns: new[] { "classe", "nome_cientifico" },
                values: new object[] { (byte)1, "Dinizia excelsa" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 12,
                column: "classe",
                value: (byte)1);

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 13,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Myrtaceae", "Corymbia citriodora", "Eucalipto-citriodora" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 14,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Myrtaceae", "Eucalyptus grandis", "Eucalipto-grandis" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 15,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Myrtaceae", "Eucalyptus saligna", "Eucalipto-saligna" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 16,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Myrtaceae", "Corymbia maculata", "Eucalipto-maculata" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 17,
                columns: new[] { "nome_cientifico", "nome_popular" },
                values: new object[] { "Vatairea sericea", "Angelim-amargoso" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 18,
                column: "nome_cientifico",
                value: "Hymenolobium petraeum");

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 19,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Vochysiaceae", "Qualea paraensis", "Mandioqueira" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 20,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Vochysiaceae", "Erisma uncinatum", "Cedrinho" });

            migrationBuilder.UpdateData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 22,
                columns: new[] { "familia", "nome_cientifico", "nome_popular" },
                values: new object[] { "Lecythidaceae", "Couratari oblongifolia", "Tauari" });
        }
    }
}
