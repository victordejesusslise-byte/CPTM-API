using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ViaPermanente.Dados;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>
/// As migrações da revisão do banco (Victor, 2026-10-23) levam os dados junto: cria o banco até a migração anterior, grava dados nas
/// tabelas que vão sumir, aplica a nova e confere que nada se perdeu.
/// </summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class MigracoesNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    public async Task InitializeAsync() => await db.Database.EnsureDeletedAsync();
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    private Task MigrarAte(string migracao) => db.GetService<IMigrator>().MigrateAsync(migracao);

    [Fact]
    public async Task A_pessoa_vai_para_o_usuario()
    {
        await MigrarAte("20261018230032_SessaoETentativas");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO PESSOA (nome, email, data_nascimento) VALUES ('Ana Teste', 'ana@teste.local', '1990-05-01');" +
            "INSERT INTO USUARIO (id_pessoa, tipo_cargo, data_cadastro, senha_hash, ativo, versao_sessao, tentativas_falhas) " +
            "SELECT id_pessoa, 'Tecnico', GETDATE(), 'hash', 1, 0, 0 FROM PESSOA WHERE email = 'ana@teste.local';");

        await MigrarAte("20261024003634_PessoaNoUsuario");

        var ana = await db.Set<Usuario>().AsNoTracking().SingleAsync();
        Assert.Equal("Ana Teste", ana.Nome);
        Assert.Equal("ana@teste.local", ana.Email);
        Assert.Equal(new DateTime(1990, 5, 1), db.Database.SqlQueryRaw<DateTime?>("SELECT CAST(data_nascimento AS datetime2) AS Value FROM USUARIO").AsEnumerable().Single());
    }

    [Fact]
    public async Task O_codigo_do_erp_vai_para_o_tamanho()
    {
        await MigrarAte("20261024003634_PessoaNoUsuario");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO CODIGO_MATERIAL (id_tamanho, classe, codigo_erp) VALUES (1, 2, 'ERP-280-2A'), (3, 1, 'ERP-400-1A');");

        await MigrarAte("20261024004958_CodigoErpNoTamanho");

        string? Codigo(int id, int classe) => db.Database.SqlQueryRaw<string?>($"SELECT codigo_erp_classe{classe} AS Value FROM TAMANHO_DORMENTE WHERE id_tamanho = {id}").AsEnumerable().Single();
        Assert.Equal("ERP-280-2A", Codigo(1, 2));
        Assert.Null(Codigo(1, 1));
        Assert.Equal("ERP-400-1A", Codigo(3, 1));
    }

    [Fact]
    public async Task O_tipo_do_leitor_vai_para_o_codigo_com_os_numeros_novos()
    {
        await MigrarAte("20261024004958_CodigoErpNoTamanho");
        // Na tabela antiga: 1 Pistola NIR, 2 Módulo do celular, 3 Carro.
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO LEITOR (id_tipo_leitor, codigo, ativo) VALUES (1, 'NIR-01', 1), (2, 'MOD-01', 1), (3, 'CAR-01', 1);");

        await MigrarAte("20261024010302_TipoLeitorNoCodigo");

        var tipos = await db.Set<Leitor>().AsNoTracking().ToDictionaryAsync(l => l.Codigo, l => l.Tipo);
        Assert.Equal(TipoLeitor.PistolaNir, tipos["NIR-01"]);
        Assert.Equal(TipoLeitor.ModuloRfid, tipos["MOD-01"]);
        Assert.Equal(TipoLeitor.Carro, tipos["CAR-01"]);
        // O banco só aceita os três tipos da lista.
        await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync("INSERT INTO LEITOR (tipo, codigo, ativo) VALUES (4, 'X-01', 1);"));
    }

    [Fact]
    public async Task O_tamanho_vai_para_o_codigo_e_o_fornecedor_ganha_o_padrao()
    {
        await MigrarAte("20261024010302_TipoLeitorNoCodigo");
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE TAMANHO_DORMENTE SET codigo_erp_classe1 = 'ERP-280-1' WHERE id_tamanho = 1;" +
            "INSERT INTO USUARIO (nome, email, tipo_cargo, data_cadastro, senha_hash, ativo, versao_sessao, tentativas_falhas) VALUES ('Ana', 'ana@teste.local', 'Supervisor', GETDATE(), 'h', 1, 0, 0);" +
            "INSERT INTO EMPRESA (nome, ativo, papel) VALUES ('TG', 1, 'Fornecedor'), ('Granoski', 1, 'Tratadora');" +
            "INSERT INTO PEDIDO (id_empresa, id_usuario, codigo, data_compra, quantidade, criado_em, status) " +
            "SELECT e.id_empresa, u.id_usuario, 'PED-1', '2026-10-01', 10, GETDATE(), 'Aberto' FROM EMPRESA e, USUARIO u WHERE e.nome = 'TG';" +
            "INSERT INTO PEDIDO_ITEM (id_pedido, id_tamanho, classe, quantidade) SELECT id_pedido, 4, 1, 10 FROM PEDIDO;");

        await MigrarAte("20261024021234_TamanhoNoCodigoEPadraoDoFornecedor");

        var padrao = await db.Set<MedidaFornecedor>().AsNoTracking().SingleAsync(); // só o fornecedor ganha padrão, não a tratadora
        Assert.Equal((0.24m, 0.17m, (int?)null), (padrao.LarguraM, padrao.AlturaM, padrao.IdEspecie));
        var item = await db.Set<PedidoItem>().AsNoTracking().SingleAsync();
        Assert.Equal((4, padrao.IdMedida), (item.IdTamanho, item.IdMedida));
        Assert.Equal("ERP-280-1", (await db.Set<Parametro>().AsNoTracking().SingleAsync(p => p.Chave == "erp_codigo_material_1_1")).Valor);
        // O banco só aceita os comprimentos da lista do código.
        await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync($"UPDATE PEDIDO_ITEM SET id_tamanho = 9 WHERE id_pedido_item = {item.IdPedidoItem};"));
    }

    [Fact]
    public async Task Os_cadastros_sem_uso_saem_do_banco()
    {
        // Colunas que o sistema nunca gravou nem leu (Victor, 2026-10-24); a equipe da troca fica (obrigatória).
        await MigrarAte("20261024033206_CadastrosSemUso");
        int Existe(string tabela, string coluna) => db.Database.SqlQueryRaw<int>(
            $"SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tabela}' AND COLUMN_NAME = '{coluna}'").AsEnumerable().Single();
        Assert.All(new[] { ("DORMENTE", "frente"), ("USUARIO", "data_nascimento"), ("ARMAZEM", "endereco"), ("SETOR", "descricao"), ("EMPRESA", "endereco"), ("EMPRESA", "telefone") },
            c => Assert.Equal(0, Existe(c.Item1, c.Item2)));
        Assert.Equal(1, Existe("TROCA", "equipe"));
    }

    /// <summary>
    /// A sigla da empresa (Victor, 2026-11-14): as que já existem ganham o que vem antes do primeiro traço do nome, se for de 2 a 6 letras
    /// ou números ("TG-Três Guri" → TG; o travessão "–" da placa também conta). As outras ficam sem sigla.
    /// </summary>
    [Fact]
    public async Task A_sigla_das_empresas_vem_do_comeco_do_nome()
    {
        await MigrarAte("20261111212549_LoteDeTroca");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO EMPRESA (nome, ativo, papel) VALUES " +
            "(N'TG-Três Guri', 1, 'Fornecedor'), (N'TG–Três Guri', 1, 'Tratadora'), (N'ab12 - Madeiras', 1, 'Fornecedor'), " +
            "(N'Granoski', 1, 'Tratadora'), (N'T-Um', 1, 'Fornecedor'), (N'ABCDEFG-Longa', 1, 'Fornecedor'), (N'Três-Guri', 1, 'Inspetora'), " +
            "(N'-Sem', 1, 'Inspetora'), (N'A B-Espaço', 1, 'Inspetora');");

        await MigrarAte("20261114230539_SiglaDaEmpresa");

        var siglas = await db.Set<Empresa>().AsNoTracking().ToDictionaryAsync(e => e.Nome, e => e.Sigla);
        Assert.Equal("TG", siglas["TG-Três Guri"]);
        Assert.Equal("TG", siglas["TG–Três Guri"]);
        Assert.Equal("AB12", siglas["ab12 - Madeiras"]);
        Assert.All(new[] { "Granoski", "T-Um", "ABCDEFG-Longa", "Três-Guri", "-Sem", "A B-Espaço" }, nome => Assert.Null(siglas[nome]));
    }

    private string Etiqueta => db.Database.GetMigrations().Single(m => m.EndsWith("_EtiquetaNaGravacaoTag"));

    /// <summary>
    /// A RFID de antes, com um exemplo de cada caso: a peça 1 com a etiqueta gravada pelo módulo; a 2 com a etiqueta sem gravação (dados de
    /// exemplo), cadastrada pela Bia; a 3 reprovada pela Bia (etiqueta inativa); a 4 com a etiqueta trocada em campo pela Bia; e uma gravação
    /// bloqueada, sem peça. A peça 12345 confere o código com mais de 4 dígitos. Devolve as peças, a empresa, o lote e os usuários.
    /// </summary>
    private async Task<(int[] Pecas, int Empresa, int Lote, int Ana, int Bia)> RfidDeAntes()
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            DECLARE @ana int, @bia int, @emp int, @ped int, @lote int, @leitor int, @p1 int, @p2 int, @p3 int, @p4 int, @r1 int, @r3 int, @r4 int;
            DECLARE @esp int = (SELECT MIN(id_especie) FROM ESPECIE);
            INSERT INTO USUARIO (nome, email, tipo_cargo, data_cadastro, senha_hash, ativo, versao_sessao, tentativas_falhas) VALUES ('Ana', 'ana@teste.local', 'Supervisor', '2026-10-01', 'h', 1, 0, 0);
            SET @ana = SCOPE_IDENTITY();
            INSERT INTO USUARIO (nome, email, tipo_cargo, data_cadastro, senha_hash, ativo, versao_sessao, tentativas_falhas) VALUES ('Bia', 'bia@teste.local', 'Tecnico', '2026-10-01', 'h', 1, 0, 0);
            SET @bia = SCOPE_IDENTITY();
            INSERT INTO EMPRESA (nome, ativo, papel) VALUES (N'TG–Três Guri', 1, 'Fornecedor');
            SET @emp = SCOPE_IDENTITY();
            INSERT INTO PEDIDO (id_empresa, id_usuario, codigo, data_compra, quantidade, criado_em, status) VALUES (@emp, @ana, 'PED-1', '2026-10-01', 10, '2026-10-01', 'Aberto');
            SET @ped = SCOPE_IDENTITY();
            INSERT INTO LOTE (id_empresa, id_pedido, codigo, data_criacao) VALUES (@emp, @ped, 'L-PED-1', '2026-10-02');
            SET @lote = SCOPE_IDENTITY();
            INSERT INTO LEITOR (tipo, codigo, ativo) VALUES (1, 'MOD-01', 1);
            SET @leitor = SCOPE_IDENTITY();
            INSERT INTO DORMENTE (id_lote) VALUES (@lote); SET @p1 = SCOPE_IDENTITY();
            INSERT INTO DORMENTE (id_lote) VALUES (@lote); SET @p2 = SCOPE_IDENTITY();
            INSERT INTO DORMENTE (id_lote) VALUES (@lote); SET @p3 = SCOPE_IDENTITY();
            INSERT INTO DORMENTE (id_lote) VALUES (@lote); SET @p4 = SCOPE_IDENTITY();
            SET IDENTITY_INSERT DORMENTE ON;
            INSERT INTO DORMENTE (id_dormente, id_lote) VALUES (12345, @lote);
            SET IDENTITY_INSERT DORMENTE OFF;

            -- O cadastro grava a primeira classificação junto com a peça: é a data do cadastro.
            INSERT INTO CLASSIFICACAO_MADEIRA (id_dormente, id_especie, id_usuario, data, metodo, registrado_em) VALUES
                (@p1, @esp, @ana, '2026-10-03T09:00:00', 'PlacaFornecedor', '2026-10-03T09:00:00'),
                (@p2, @esp, @bia, '2026-10-05T10:00:00', 'PlacaFornecedor', '2026-10-05T10:00:00'),
                (@p3, @esp, @ana, '2026-10-03T09:05:00', 'PlacaFornecedor', '2026-10-03T09:05:00'),
                (@p4, @esp, @ana, '2026-10-03T09:10:00', 'PlacaFornecedor', '2026-10-03T09:10:00');
            INSERT INTO VERIFICACAO_QUALIDADE (id_dormente, id_usuario, data, ataque_biologico, rachadura, placa_antirrachante, placa_confere, resultado, motivo, foto_arquivo, destino, registrado_em)
                VALUES (@p3, @bia, '2026-10-04T15:00:00', 'Nenhum', 'Grave', 1, 1, 'Reprovada', 'Rachadura grave', 'fotos/p3.jpg', 'Devolucao', '2026-10-04T15:00:00');

            INSERT INTO RFID (id_dormente, tag_rfid, ativo) VALUES (@p1, 'BR0000000001', 1); SET @r1 = SCOPE_IDENTITY();
            INSERT INTO RFID (id_dormente, tag_rfid, ativo) VALUES (@p2, 'BR0000000002', 1);
            INSERT INTO RFID (id_dormente, tag_rfid, ativo) VALUES (@p3, 'BR0000000003', 0); SET @r3 = SCOPE_IDENTITY();
            INSERT INTO RFID (id_dormente, tag_rfid, ativo) VALUES (@p4, 'BR0000000004', 0); SET @r4 = SCOPE_IDENTITY();
            INSERT INTO RFID (id_dormente, tag_rfid, ativo, vinculada_em, id_usuario_vinculou) VALUES (@p4, 'BR0000000005', 1, '2026-10-08T11:00:00', @bia);

            INSERT INTO GRAVACAO_TAG (id_rfid, tag_gravada, tag_relida, resultado, tags_vistas, id_modo, id_leitor, id_usuario, data, registrado_em) VALUES
                (@r1, 'BR0000000001', 'BR0000000001', 'Ok', 1, 2, @leitor, @ana, '2026-10-03T09:00:00', '2026-10-03T09:00:00'),
                (NULL, 'BR0000000006', NULL, 'Bloqueada', 2, 2, @leitor, @ana, '2026-10-03T09:02:00', '2026-10-03T09:02:00'),
                (@r3, 'BR0000000003', 'BR0000000003', 'Ok', 1, 2, @leitor, @ana, '2026-10-03T09:05:00', '2026-10-03T09:05:00'),
                (@r4, 'BR0000000004', 'BR0000000004', 'Ok', 1, 2, @leitor, @ana, '2026-10-03T09:10:00', '2026-10-03T09:10:00');
            """);
        var pecas = db.Database.SqlQueryRaw<int>("SELECT id_dormente AS Value FROM DORMENTE").AsEnumerable().Order().ToArray();
        int Id(string sql) => db.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
        return (pecas, Id("SELECT id_empresa AS Value FROM EMPRESA"), Id("SELECT id_lote AS Value FROM LOTE"),
            Id("SELECT id_usuario AS Value FROM USUARIO WHERE nome = 'Ana'"), Id("SELECT id_usuario AS Value FROM USUARIO WHERE nome = 'Bia'"));
    }

    /// <summary>
    /// Etiqueta na GRAVACAO_TAG, sem a RFID (Victor, 2026-11-14): a etiqueta já gravada ganha a peça e a marca vigente igual ao ativo; a que não
    /// tinha gravação entra como Gravada (ou Ligada, se foi ligada em campo), com a data e quem fez; a inativa ganha uma Desligada. A RFID sai,
    /// e o código da peça é gravado no DORMENTE.
    /// </summary>
    [Fact]
    public async Task A_etiqueta_da_RFID_vai_para_a_GRAVACAO_TAG_e_o_codigo_para_o_DORMENTE()
    {
        await MigrarAte("20261114230539_SiglaDaEmpresa");
        var (pecas, empresa, lote, ana, bia) = await RfidDeAntes();
        var (p1, p2, p3, p4, p12345) = (pecas[0], pecas[1], pecas[2], pecas[3], pecas[4]);

        await MigrarAte(Etiqueta);

        int Existe(string tabela, string coluna = "%") => db.Database.SqlQueryRaw<int>(
            $"SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tabela}' AND COLUMN_NAME LIKE '{coluna}'").AsEnumerable().Single();
        Assert.Equal(0, Existe("RFID"));
        Assert.Equal(0, Existe("GRAVACAO_TAG", "id_rfid"));

        var linhas = await db.Set<GravacaoTag>().AsNoTracking().OrderBy(g => g.IdGravacao).ToListAsync();
        Assert.Equal(8, linhas.Count);
        GravacaoTag Linha(string tag, TipoGravacao tipo) => linhas.Single(g => g.TagGravada == tag && g.Tipo == tipo);

        // Vigentes: uma por etiqueta ativa da RFID.
        Assert.Equal([("BR0000000001", p1), ("BR0000000002", p2), ("BR0000000005", p4)],
            linhas.Where(g => g.Vigente).OrderBy(g => g.TagGravada).Select(g => (g.TagGravada, g.IdDormente!.Value)));

        // Já gravada pelo módulo: a mesma linha, com a peça.
        var g1 = Linha("BR0000000001", TipoGravacao.Gravada);
        Assert.Equal((linhas[0].IdGravacao, p1, ResultadoGravacao.Ok, (int?)2), (g1.IdGravacao, g1.IdDormente!.Value, g1.Resultado, g1.IdModo));

        // Sem gravação: entra Gravada, com a data do cadastro e quem cadastrou.
        var g2 = Linha("BR0000000002", TipoGravacao.Gravada);
        Assert.Equal((p2, ResultadoGravacao.Ok, bia, new DateTime(2026, 10, 5, 10, 0, 0), "trazida da RFID"),
            (g2.IdDormente!.Value, g2.Resultado, g2.IdUsuario, g2.Data, g2.Observacao));
        Assert.Null(g2.IdLeitor);
        Assert.Null(g2.IdModo);

        // Reprovada: a gravação perde a marca e entra a Desligada, na data da reprovação.
        Assert.False(Linha("BR0000000003", TipoGravacao.Gravada).Vigente);
        var d3 = Linha("BR0000000003", TipoGravacao.Desligada);
        Assert.Equal((p3, bia, new DateTime(2026, 10, 4, 15, 0, 0), "desligada antes de 2026-11-14 (trazida da RFID)", false),
            (d3.IdDormente!.Value, d3.IdUsuario, d3.Data, d3.Observacao, d3.Vigente));

        // Trocada em campo: a velha perde a marca e é desligada quando a nova foi ligada; a nova entra Ligada, com quem ligou.
        Assert.Equal(p4, Linha("BR0000000004", TipoGravacao.Gravada).IdDormente);
        var d4 = Linha("BR0000000004", TipoGravacao.Desligada);
        Assert.Equal((p4, bia, new DateTime(2026, 10, 8, 11, 0, 0)), (d4.IdDormente!.Value, d4.IdUsuario, d4.Data));
        var l5 = Linha("BR0000000005", TipoGravacao.Ligada);
        Assert.Equal((p4, ResultadoGravacao.Ok, bia, new DateTime(2026, 10, 8, 11, 0, 0), true),
            (l5.IdDormente!.Value, l5.Resultado, l5.IdUsuario, l5.Data, l5.Vigente));
        Assert.True(d4.IdGravacao < l5.IdGravacao); // no histórico, a velha sai antes de a nova entrar

        // A gravação bloqueada continua sem peça.
        var bloqueada = Linha("BR0000000006", TipoGravacao.Gravada);
        Assert.Equal((ResultadoGravacao.Bloqueada, (int?)null, false), (bloqueada.Resultado, bloqueada.IdDormente, bloqueada.Vigente));

        // O código da peça, no mesmo formato do sistema.
        var codigos = await db.Set<Dormente>().AsNoTracking().ToDictionaryAsync(d => d.IdDormente, d => d.Codigo);
        Assert.All(pecas, id => Assert.Equal(Dormente.MontarCodigo(empresa, lote, id), codigos[id]));
        Assert.EndsWith("-12345", codigos[p12345]);
        Assert.Equal(ana, g1.IdUsuario);
    }

    /// <summary>A conferência da migração: se o número de vigentes não bate com o de ativas da RFID, ela não termina.</summary>
    [Fact]
    public async Task A_migracao_da_etiqueta_para_se_as_vigentes_nao_batem_com_as_ativas()
    {
        await MigrarAte(Etiqueta);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE RFID (ativo bit NOT NULL); INSERT INTO RFID (ativo) VALUES (1);");

        var erro = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync(Dados.Migrations.EtiquetaNaGravacaoTag.Conferencia));

        Assert.Contains("vigentes", erro.Message);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM RFID;");
        await db.Database.ExecuteSqlRawAsync(Dados.Migrations.EtiquetaNaGravacaoTag.Conferencia); // 0 e 0: passa
    }

    /// <summary>Voltar a migração devolve a RFID como era: as etiquetas, as ativas, o vínculo em campo e a ligação com a gravação.</summary>
    [Fact]
    public async Task Voltar_a_migracao_da_etiqueta_devolve_a_RFID()
    {
        await MigrarAte("20261114230539_SiglaDaEmpresa");
        var (pecas, _, _, _, bia) = await RfidDeAntes();
        string Rfid() => string.Join(";", db.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(r.id_dormente, ',', r.tag_rfid, ',', r.ativo, ',', CONVERT(varchar(19), r.vinculada_em, 126), ',', r.id_usuario_vinculou, ',', " +
            "(SELECT COUNT(*) FROM GRAVACAO_TAG g WHERE g.id_rfid = r.id_rfid)) AS Value FROM RFID r ORDER BY r.tag_rfid").AsEnumerable());
        var antes = Rfid();

        await MigrarAte(Etiqueta);
        await MigrarAte("20261114230539_SiglaDaEmpresa");

        Assert.Equal(antes, Rfid());
        Assert.Contains($"{pecas[3]},BR0000000005,1,2026-10-08T11:00:00,{bia},0", antes);
        Assert.Equal(4, db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM GRAVACAO_TAG").AsEnumerable().Single());
    }
}
