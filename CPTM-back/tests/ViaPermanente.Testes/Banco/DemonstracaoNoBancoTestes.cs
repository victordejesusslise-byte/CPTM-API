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
using ViaPermanente.Dominio.Via.Models;

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

    [Fact]
    public async Task A_passagem_do_exemplo_aponta_as_duas_estacoes_e_tem_o_caminho_do_GPS()
    {
        var supervisor = await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Supervisor de teste", Email = "supervisor@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor
        });
        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(supervisor.Id);
        var demonstracao = new DemonstracaoService(new ApresentacaoRepository(db));
        await demonstracao.Criar(supervisor.Id);

        db.ChangeTracker.Clear();
        var passagem = await db.Set<Passagem>().AsNoTracking().SingleAsync(p => p.Codigo.StartsWith("DEMO-"));
        var trecho = await db.Set<Trecho>().AsNoTracking().SingleAsync(t => t.IdEstacaoInicio == passagem.IdEstacaoInicio);
        Assert.Equal(trecho.IdEstacaoFim, passagem.IdEstacaoFim);

        // O caminho: o primeiro ponto vale o km da Estação A e o último o da B, como a passagem do sistema calcula.
        var pontos = await db.Set<PontoPassagem>().AsNoTracking().Where(p => p.IdPassagem == passagem.IdPassagem).OrderBy(p => p.Data).ToListAsync();
        Assert.True(pontos.Count >= 5);
        Assert.Equal(212.000m, pontos[0].KmCalculado);
        Assert.Equal(215.000m, pontos[^1].KmCalculado);

        // As leituras trazem o GPS, e o km calculado fica perto do km do projeto da peça, mas não igual: o GPS só aproxima.
        var leituras = await (from l in db.Set<LeituraVia>().AsNoTracking()
                              join d in db.Set<Dormente>().AsNoTracking() on l.IdDormente equals d.IdDormente
                              where l.IdPassagem == passagem.IdPassagem
                              select new { l.Latitude, l.Longitude, l.KmMedido, d.KmProjeto }).ToListAsync();
        Assert.Equal(3, leituras.Count);
        Assert.All(leituras, l => { Assert.NotNull(l.Latitude); Assert.NotNull(l.Longitude); Assert.InRange(Math.Abs(l.KmMedido!.Value - l.KmProjeto!.Value), 0m, 0.010m); });
        Assert.Contains(leituras, l => l.KmMedido != l.KmProjeto);

        // A limpeza apaga o caminho junto com a passagem.
        await demonstracao.Apagar();
        Assert.Equal(0, await db.Set<PontoPassagem>().CountAsync());
    }
}
