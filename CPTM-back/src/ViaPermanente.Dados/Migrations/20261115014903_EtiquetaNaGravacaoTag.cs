using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViaPermanente.Dados.Migrations
{
    /// <summary>
    /// Etiqueta na GRAVACAO_TAG, sem a RFID (Victor, 2026-11-14): a GRAVACAO_TAG vira o histórico da etiqueta (Gravada, Ligada, Desligada),
    /// com a peça e a marca vigente; os dados da RFID vão para ela, a migração confere as vigentes e a RFID sai. O código da peça
    /// (DM-E01-L01-0028) ganha a coluna DORMENTE.codigo.
    /// </summary>
    public partial class EtiquetaNaGravacaoTag : Migration
    {
        /// <summary>
        /// A conferência: o número de etiquetas vigentes na GRAVACAO_TAG tem de ser igual ao de ativas na RFID. Se não bater, a migração
        /// para (THROW) e nada fica gravado.
        /// </summary>
        public const string Conferencia =
            """
            DECLARE @ativas int = (SELECT COUNT(*) FROM RFID WHERE ativo = 1);
            DECLARE @vigentes int = (SELECT COUNT(*) FROM GRAVACAO_TAG WHERE vigente = 1);
            IF @vigentes <> @ativas
            BEGIN
                DECLARE @erro nvarchar(400) = CONCAT(N'A migração da etiqueta não confere: ', @vigentes, N' vigentes na GRAVACAO_TAG e ',
                    @ativas, N' ativas na RFID.');
                THROW 50000, @erro, 1;
            END
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. As colunas novas. As linhas que já existem são gravações do módulo: o tipo começa como Gravada.
            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "GRAVACAO_TAG",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Gravada");

            migrationBuilder.AddColumn<int>(
                name: "id_dormente",
                table: "GRAVACAO_TAG",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "vigente",
                table: "GRAVACAO_TAG",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "observacao",
                table: "GRAVACAO_TAG",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id_modo",
                table: "GRAVACAO_TAG",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "id_leitor",
                table: "GRAVACAO_TAG",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "codigo",
                table: "DORMENTE",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // O código das peças que já existem, no mesmo formato do Dormente.MontarCodigo: DM-E{empresa:00}-L{lote:00}-{peça:0000}.
            migrationBuilder.Sql(
                """
                UPDATE d SET codigo = CONCAT('DM-E', FORMAT(l.id_empresa, '00', 'en-US'), '-L', FORMAT(d.id_lote, '00', 'en-US'), '-',
                    FORMAT(d.id_dormente, '0000', 'en-US'))
                FROM DORMENTE d
                JOIN LOTE l ON l.id_lote = d.id_lote;
                """);

            // 2a. As etiquetas já gravadas: a linha da gravação recebe a peça e o tipo Gravada; a gravação ok da etiqueta (a primeira, se
            // houvesse mais de uma) fica vigente igual ao ativo da RFID.
            migrationBuilder.Sql(
                """
                UPDATE g SET id_dormente = r.id_dormente, tipo = 'Gravada',
                    vigente = CASE WHEN g.resultado = 'Ok' AND g.id_gravacao = (SELECT MIN(g2.id_gravacao) FROM GRAVACAO_TAG g2
                                   WHERE g2.id_rfid = r.id_rfid AND g2.resultado = 'Ok') THEN r.ativo ELSE 0 END
                FROM GRAVACAO_TAG g
                JOIN RFID r ON r.id_rfid = g.id_rfid;
                """);

            // 2b. As etiquetas sem gravação entram como linha nova: Ligada, se foram ligadas em campo; Gravada, se não (os dados de exemplo).
            // Sempre ok, com a data e quem ligou; sem isso, a data e quem fez o cadastro da peça (a primeira classificação, gravada junto com
            // ela), depois a chegada, e por fim o primeiro usuário.
            // 2c. As inativas ganham uma Desligada: na hora em que a etiqueta nova foi ligada no lugar dela, por quem ligou; senão, na
            // reprovação da peça, por quem reprovou; senão, na data da própria etiqueta.
            // Uma inserção só, em ordem de data: a Desligada da troca entra antes da Ligada que tomou o lugar dela.
            migrationBuilder.Sql(
                """
                WITH etiqueta AS (
                    SELECT r.id_rfid, r.id_dormente, r.tag_rfid, r.ativo, r.vinculada_em, gr.id_gravacao,
                           COALESCE(gr.id_usuario, r.id_usuario_vinculou, cad.id_usuario, rec.id_usuario, (SELECT MIN(u.id_usuario) FROM USUARIO u)) AS id_usuario,
                           COALESCE(gr.data, r.vinculada_em, cad.data, rec.data_recebimento, SYSDATETIME()) AS data
                    FROM RFID r
                    OUTER APPLY (SELECT TOP 1 g.id_gravacao, g.id_usuario, g.data FROM GRAVACAO_TAG g
                                 WHERE g.id_rfid = r.id_rfid AND g.resultado = 'Ok' ORDER BY g.id_gravacao) gr
                    OUTER APPLY (SELECT TOP 1 c.id_usuario, c.data FROM CLASSIFICACAO_MADEIRA c
                                 WHERE c.id_dormente = r.id_dormente ORDER BY c.id_classificacao) cad
                    OUTER APPLY (SELECT TOP 1 re.id_usuario, re.data_recebimento FROM DORMENTE d
                                 JOIN FEIXE f ON f.id_feixe = d.id_feixe
                                 JOIN RECEBIMENTO re ON re.id_recebimento = f.id_recebimento
                                 WHERE d.id_dormente = r.id_dormente) rec
                ),
                linha AS (
                    SELECT 1 AS ordem, CASE WHEN e.vinculada_em IS NULL THEN 'Gravada' ELSE 'Ligada' END AS tipo, e.id_dormente, e.tag_rfid,
                           1 AS tags_vistas, e.ativo AS vigente,
                           CASE WHEN e.vinculada_em IS NULL THEN N'trazida da RFID' ELSE N'ligada em campo (trazida da RFID)' END AS observacao,
                           e.id_usuario, e.data
                    FROM etiqueta e
                    WHERE e.id_gravacao IS NULL
                    UNION ALL
                    SELECT CASE WHEN nova.vinculada_em IS NOT NULL THEN 0 ELSE 2 END, 'Desligada', e.id_dormente, e.tag_rfid,
                           0, CAST(0 AS bit), N'desligada antes de 2026-11-14 (trazida da RFID)',
                           CASE WHEN nova.vinculada_em IS NOT NULL THEN COALESCE(nova.id_usuario_vinculou, e.id_usuario)
                                WHEN rep.data IS NOT NULL THEN rep.id_usuario
                                ELSE e.id_usuario END,
                           COALESCE(nova.vinculada_em, rep.data, e.data)
                    FROM etiqueta e
                    OUTER APPLY (SELECT TOP 1 n.vinculada_em, n.id_usuario_vinculou FROM RFID n
                                 WHERE n.id_dormente = e.id_dormente AND n.id_rfid > e.id_rfid AND n.vinculada_em IS NOT NULL ORDER BY n.id_rfid) nova
                    OUTER APPLY (SELECT TOP 1 v.id_usuario, v.data FROM VERIFICACAO_QUALIDADE v
                                 WHERE v.id_dormente = e.id_dormente AND v.resultado = 'Reprovada' ORDER BY v.id_verificacao) rep
                    WHERE e.ativo = 0
                )
                INSERT INTO GRAVACAO_TAG (tipo, id_dormente, tag_gravada, resultado, tags_vistas, vigente, observacao, id_usuario, data, registrado_em, recebido_em)
                SELECT l.tipo, l.id_dormente, l.tag_rfid, 'Ok', l.tags_vistas, l.vigente, l.observacao, l.id_usuario, l.data, l.data, l.data
                FROM linha l
                ORDER BY l.data, l.ordem, l.tag_rfid;
                """);

            // 4. A conferência, antes de a RFID sair.
            migrationBuilder.Sql(Conferencia);

            // 3. A RFID sai, junto com a coluna que apontava para ela.
            migrationBuilder.DropForeignKey(
                name: "FK_GRAVACAO_TAG_RFID_id_rfid",
                table: "GRAVACAO_TAG");

            migrationBuilder.DropIndex(
                name: "IX_GRAVACAO_TAG_id_rfid",
                table: "GRAVACAO_TAG");

            migrationBuilder.DropColumn(
                name: "id_rfid",
                table: "GRAVACAO_TAG");

            migrationBuilder.DropTable(
                name: "RFID");

            // As regras da etiqueta vigente, no banco (criadas depois dos dados: conferem também o que veio da RFID).
            migrationBuilder.CreateIndex(
                name: "UX_GRAVACAO_TAG_tag_vigente",
                table: "GRAVACAO_TAG",
                column: "tag_gravada",
                unique: true,
                filter: "[vigente] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_GRAVACAO_TAG_uma_vigente_por_peca",
                table: "GRAVACAO_TAG",
                column: "id_dormente",
                unique: true,
                filter: "[vigente] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GRAVACAO_TAG_vigente",
                table: "GRAVACAO_TAG",
                sql: "[vigente] = 0 OR ([id_dormente] IS NOT NULL AND ([tipo] = 'Ligada' OR ([tipo] = 'Gravada' AND [resultado] = 'Ok')))");

            migrationBuilder.CreateIndex(
                name: "IX_DORMENTE_codigo",
                table: "DORMENTE",
                column: "codigo",
                unique: true,
                filter: "[codigo] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_GRAVACAO_TAG_DORMENTE_id_dormente",
                table: "GRAVACAO_TAG",
                column: "id_dormente",
                principalTable: "DORMENTE",
                principalColumn: "id_dormente",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GRAVACAO_TAG_DORMENTE_id_dormente",
                table: "GRAVACAO_TAG");

            migrationBuilder.DropIndex(
                name: "UX_GRAVACAO_TAG_tag_vigente",
                table: "GRAVACAO_TAG");

            migrationBuilder.DropIndex(
                name: "UX_GRAVACAO_TAG_uma_vigente_por_peca",
                table: "GRAVACAO_TAG");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GRAVACAO_TAG_vigente",
                table: "GRAVACAO_TAG");

            migrationBuilder.DropIndex(
                name: "IX_DORMENTE_codigo",
                table: "DORMENTE");

            migrationBuilder.CreateTable(
                name: "RFID",
                columns: table => new
                {
                    id_rfid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_dormente = table.Column<int>(type: "int", nullable: false),
                    tag_rfid = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ativo = table.Column<bool>(type: "bit", nullable: false),
                    vinculada_em = table.Column<DateTime>(type: "datetime2", nullable: true),
                    id_usuario_vinculou = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RFID", x => x.id_rfid);
                    table.ForeignKey(
                        name: "FK_RFID_DORMENTE_id_dormente",
                        column: x => x.id_dormente,
                        principalTable: "DORMENTE",
                        principalColumn: "id_dormente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RFID_USUARIO_id_usuario_vinculou",
                        column: x => x.id_usuario_vinculou,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<int>(
                name: "id_rfid",
                table: "GRAVACAO_TAG",
                type: "int",
                nullable: true);

            // As etiquetas voltam para a RFID (a Gravada ok com peça e a Ligada, ativa se vigente; a Ligada com quem e quando ligou), a
            // gravação do módulo volta a apontar para a etiqueta, e saem as linhas que a GRAVACAO_TAG de antes não tinha (Ligada,
            // Desligada e as etiquetas sem leitura, que vieram da RFID).
            migrationBuilder.Sql(
                """
                INSERT INTO RFID (id_dormente, tag_rfid, ativo, vinculada_em, id_usuario_vinculou)
                SELECT g.id_dormente, g.tag_gravada, g.vigente,
                       CASE WHEN g.tipo = 'Ligada' THEN g.data END, CASE WHEN g.tipo = 'Ligada' THEN g.id_usuario END
                FROM GRAVACAO_TAG g
                WHERE g.id_dormente IS NOT NULL AND (g.tipo = 'Ligada' OR (g.tipo = 'Gravada' AND g.resultado = 'Ok'))
                ORDER BY g.id_gravacao;

                UPDATE g SET id_rfid = r.id_rfid
                FROM GRAVACAO_TAG g
                JOIN RFID r ON r.id_dormente = g.id_dormente AND r.tag_rfid = g.tag_gravada
                WHERE g.tipo = 'Gravada' AND g.resultado = 'Ok' AND g.id_modo IS NOT NULL AND g.id_leitor IS NOT NULL;

                DELETE FROM GRAVACAO_TAG WHERE tipo <> 'Gravada' OR id_modo IS NULL OR id_leitor IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "id_dormente",
                table: "GRAVACAO_TAG");

            migrationBuilder.DropColumn(
                name: "observacao",
                table: "GRAVACAO_TAG");

            migrationBuilder.DropColumn(
                name: "tipo",
                table: "GRAVACAO_TAG");

            migrationBuilder.DropColumn(
                name: "vigente",
                table: "GRAVACAO_TAG");

            migrationBuilder.DropColumn(
                name: "codigo",
                table: "DORMENTE");

            migrationBuilder.AlterColumn<int>(
                name: "id_modo",
                table: "GRAVACAO_TAG",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id_leitor",
                table: "GRAVACAO_TAG",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GRAVACAO_TAG_id_rfid",
                table: "GRAVACAO_TAG",
                column: "id_rfid");

            migrationBuilder.CreateIndex(
                name: "IX_RFID_id_usuario_vinculou",
                table: "RFID",
                column: "id_usuario_vinculou");

            migrationBuilder.CreateIndex(
                name: "IX_RFID_tag_rfid",
                table: "RFID",
                column: "tag_rfid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_RFID_uma_ativa_por_peca",
                table: "RFID",
                column: "id_dormente",
                unique: true,
                filter: "[ativo] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_GRAVACAO_TAG_RFID_id_rfid",
                table: "GRAVACAO_TAG",
                column: "id_rfid",
                principalTable: "RFID",
                principalColumn: "id_rfid",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
