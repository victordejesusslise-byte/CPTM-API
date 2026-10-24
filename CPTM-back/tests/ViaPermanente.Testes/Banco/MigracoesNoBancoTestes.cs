using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ViaPermanente.Dados;
using ViaPermanente.Dominio.Catalogos.Models;
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

        var tamanhos = await db.Set<TamanhoDormente>().AsNoTracking().ToDictionaryAsync(t => t.IdTamanho);
        Assert.Equal("ERP-280-2A", tamanhos[1].CodigoErpClasse2);
        Assert.Null(tamanhos[1].CodigoErpClasse1);
        Assert.Equal("ERP-400-1A", tamanhos[3].CodigoErpClasse1);
    }
}
