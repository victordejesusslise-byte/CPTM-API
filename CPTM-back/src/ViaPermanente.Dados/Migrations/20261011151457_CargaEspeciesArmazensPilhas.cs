using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ViaPermanente.Dados.Migrations
{
    /// <inheritdoc />
    public partial class CargaEspeciesArmazensPilhas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ARMAZEM",
                columns: new[] { "id_armazem", "ativo", "endereco", "km", "linha", "nome" },
                values: new object[,]
                {
                    { 1, true, null, 199.800m, "9-Esmeralda", "Presidente Altino" },
                    { 2, true, null, 209.600m, "9-Esmeralda", "Pinheiros" },
                    { 3, true, null, 219.300m, "9-Esmeralda", "Santo Amaro" },
                    { 4, true, null, 224.900m, "9-Esmeralda", "Jurubatuba" },
                    { 5, true, null, 231.500m, "9-Esmeralda", "Grajaú" }
                });

            migrationBuilder.InsertData(
                table: "ESPECIE",
                columns: new[] { "id_especie", "ativo", "classe", "familia", "nome_cientifico", "nome_popular" },
                values: new object[,]
                {
                    { 1, true, (byte)1, "Lauraceae", "Ocotea neesiana", "Canela" },
                    { 2, true, (byte)1, "Bignoniaceae", "Handroanthus serratifolius", "Ipê-amarelo" },
                    { 3, true, (byte)1, "Fabaceae", "Hymenaea courbaril", "Jatobá" },
                    { 4, true, (byte)1, "Fabaceae", "Dipteryx odorata", "Cumaru" },
                    { 5, true, (byte)1, "Sapotaceae", "Manilkara huberi", "Maçaranduba" },
                    { 6, true, (byte)1, "Anacardiaceae", "Myracrodruon urundeuva", "Aroeira" },
                    { 7, true, (byte)1, "Anacardiaceae", "Astronium lecointei", "Muiracatiara" },
                    { 8, true, (byte)1, "Bignoniaceae", "Handroanthus impetiginosus", "Ipê-roxo" },
                    { 9, true, (byte)1, "Fabaceae", "Bowdichia nitida", "Sucupira" },
                    { 10, true, (byte)1, "Lauraceae", "Mezilaurus itauba", "Itaúba" },
                    { 11, true, (byte)1, "Fabaceae", "Dinizia excelsa", "Angelim-vermelho" },
                    { 12, true, (byte)1, "Goupiaceae", "Goupia glabra", "Cupiúba" },
                    { 13, true, (byte)2, "Myrtaceae", "Corymbia citriodora", "Eucalipto-citriodora" },
                    { 14, true, (byte)2, "Myrtaceae", "Eucalyptus grandis", "Eucalipto-grandis" },
                    { 15, true, (byte)2, "Myrtaceae", "Eucalyptus saligna", "Eucalipto-saligna" },
                    { 16, true, (byte)2, "Myrtaceae", "Corymbia maculata", "Eucalipto-maculata" },
                    { 17, true, (byte)2, "Fabaceae", "Vatairea sericea", "Angelim-amargoso" },
                    { 18, true, (byte)2, "Fabaceae", "Hymenolobium petraeum", "Angelim-pedra" },
                    { 19, true, (byte)2, "Vochysiaceae", "Qualea paraensis", "Mandioqueira" },
                    { 20, true, (byte)2, "Vochysiaceae", "Erisma uncinatum", "Cedrinho" },
                    { 21, true, (byte)2, "Fabaceae", "Apuleia leiocarpa", "Garapa" },
                    { 22, true, (byte)2, "Lecythidaceae", "Couratari oblongifolia", "Tauari" }
                });

            migrationBuilder.InsertData(
                table: "PILHA",
                columns: new[] { "id_pilha", "ativo", "classe", "codigo", "id_armazem", "id_tamanho", "setor" },
                values: new object[,]
                {
                    { 1, true, (byte)1, "A01", 1, 1, "A" },
                    { 2, true, (byte)2, "A02", 1, 1, "A" },
                    { 3, true, (byte)1, "B01", 1, 2, "B" },
                    { 4, true, (byte)2, "B02", 1, 2, "B" },
                    { 5, true, (byte)1, "C01", 1, 3, "C" },
                    { 6, true, (byte)2, "C02", 1, 3, "C" },
                    { 7, true, (byte)1, "D01", 1, 4, "D" },
                    { 8, true, (byte)2, "D02", 1, 4, "D" },
                    { 9, true, (byte)1, "A01", 2, 1, "A" },
                    { 10, true, (byte)2, "A02", 2, 1, "A" },
                    { 11, true, (byte)1, "B01", 2, 2, "B" },
                    { 12, true, (byte)2, "B02", 2, 2, "B" },
                    { 13, true, (byte)1, "C01", 2, 3, "C" },
                    { 14, true, (byte)2, "C02", 2, 3, "C" },
                    { 15, true, (byte)1, "D01", 2, 4, "D" },
                    { 16, true, (byte)2, "D02", 2, 4, "D" },
                    { 17, true, (byte)1, "A01", 3, 1, "A" },
                    { 18, true, (byte)2, "A02", 3, 1, "A" },
                    { 19, true, (byte)1, "B01", 3, 2, "B" },
                    { 20, true, (byte)2, "B02", 3, 2, "B" },
                    { 21, true, (byte)1, "C01", 3, 3, "C" },
                    { 22, true, (byte)2, "C02", 3, 3, "C" },
                    { 23, true, (byte)1, "D01", 3, 4, "D" },
                    { 24, true, (byte)2, "D02", 3, 4, "D" },
                    { 25, true, (byte)1, "A01", 4, 1, "A" },
                    { 26, true, (byte)2, "A02", 4, 1, "A" },
                    { 27, true, (byte)1, "B01", 4, 2, "B" },
                    { 28, true, (byte)2, "B02", 4, 2, "B" },
                    { 29, true, (byte)1, "C01", 4, 3, "C" },
                    { 30, true, (byte)2, "C02", 4, 3, "C" },
                    { 31, true, (byte)1, "D01", 4, 4, "D" },
                    { 32, true, (byte)2, "D02", 4, 4, "D" },
                    { 33, true, (byte)1, "A01", 5, 1, "A" },
                    { 34, true, (byte)2, "A02", 5, 1, "A" },
                    { 35, true, (byte)1, "B01", 5, 2, "B" },
                    { 36, true, (byte)2, "B02", 5, 2, "B" },
                    { 37, true, (byte)1, "C01", 5, 3, "C" },
                    { 38, true, (byte)2, "C02", 5, 3, "C" },
                    { 39, true, (byte)1, "D01", 5, 4, "D" },
                    { 40, true, (byte)2, "D02", 5, 4, "D" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "ESPECIE",
                keyColumn: "id_especie",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "PILHA",
                keyColumn: "id_pilha",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "ARMAZEM",
                keyColumn: "id_armazem",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ARMAZEM",
                keyColumn: "id_armazem",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ARMAZEM",
                keyColumn: "id_armazem",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ARMAZEM",
                keyColumn: "id_armazem",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ARMAZEM",
                keyColumn: "id_armazem",
                keyValue: 5);
        }
    }
}
