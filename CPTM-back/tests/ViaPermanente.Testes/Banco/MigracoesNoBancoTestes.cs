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
        Assert.Equal(new DateOnly(1990, 5, 1), ana.DataNascimento);
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
}
