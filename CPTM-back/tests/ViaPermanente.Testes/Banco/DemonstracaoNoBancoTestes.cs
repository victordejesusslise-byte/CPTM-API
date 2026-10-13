using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>O banco de demonstração no SQL Server de verdade: cria, não deixa criar duas vezes e a limpeza apaga só o DEMO-.</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class DemonstracaoNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();

    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Cria_a_demonstracao_e_a_limpeza_deixa_o_resto_do_banco_como_estava()
    {
        var supervisor = await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Supervisor de teste", Email = "supervisor@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor
        });
        var exemplo = await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(supervisor.Id);
        var demonstracao = new DemonstracaoService(new ApresentacaoRepository(db));

        var criada = await demonstracao.Criar(supervisor.Id);

        Assert.Equal(3, await db.Set<Pedido>().CountAsync(p => p.Codigo.StartsWith("DEMO-")));
        Assert.Equal(10, criada.Pecas);
        Assert.Equal(3, await db.Set<Alerta>().CountAsync());
        Assert.Equal([StatusTroca.Pedida, StatusTroca.Aprovada, StatusTroca.Separada], await db.Set<Troca>().OrderBy(t => t.IdTroca).Select(t => t.Status).ToListAsync());
        Assert.Equal(3, await db.Set<Dormente>().CountAsync(d => d.IdTrecho != null));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => demonstracao.Criar(supervisor.Id));

        db.ChangeTracker.Clear();
        var apagada = await demonstracao.Apagar();

        Assert.Equal(3, apagada.Pedidos);
        Assert.Equal(0, await db.Set<Pedido>().CountAsync(p => p.Codigo.StartsWith("DEMO-")));
        Assert.Equal(0, await db.Set<Alerta>().CountAsync());
        Assert.Equal(0, await db.Set<Troca>().CountAsync());
        Assert.Equal(1, await db.Set<Pedido>().CountAsync());
        Assert.Equal(28, await db.Set<Dormente>().CountAsync());
        Assert.Equal(exemplo.CodigoPedido, await db.Set<Pedido>().Select(p => p.Codigo).SingleAsync());
    }
}
