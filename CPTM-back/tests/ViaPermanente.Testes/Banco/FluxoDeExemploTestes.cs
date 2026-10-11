using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Repositories;
using ViaPermanente.Dominio.Dtos.Usuarios;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Testes.Banco;

/// <summary>
/// Teste com o SQL Server de verdade (BancoDeTeste): roda o exemplo do fluxo pelas cinco etapas.
/// </summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class FluxoDeExemploTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();

    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Exemplo_passa_pelas_cinco_etapas()
    {
        var supervisor = await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Supervisor de teste", Email = "supervisor@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor
        });
        var apresentacao = new ApresentacaoService(new ApresentacaoRepository(db));

        var criado = await apresentacao.CriarExemplo(supervisor.Id);

        Assert.Equal("Santo Amaro", criado.Armazem);
        Assert.Equal("A01", criado.Pilha);
        Assert.All(criado.Etapas, e => Assert.True(e.Feita, e.Nome));
        Assert.Equal(26, await db.Set<Dormente>().CountAsync(d => d.IdPilha != null));
        Assert.Equal(2, await db.Set<Rfid>().CountAsync(r => !r.Ativo));
        Assert.Equal(28, await db.Set<Rfid>().Select(r => r.TagRfid).Distinct().CountAsync());
    }

    [Fact]
    public async Task Segundo_exemplo_continua_a_sequencia_das_tags()
    {
        var supervisor = await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Supervisor de teste", Email = "supervisor@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor
        });

        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(supervisor.Id);
        db.ChangeTracker.Clear();
        await Task.Delay(1100); // o código do pedido usa os segundos
        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(supervisor.Id);

        Assert.Equal("BR0000000056", await db.Set<Rfid>().MaxAsync(r => r.TagRfid));
        Assert.Equal(56, await db.Set<Dormente>().CountAsync());
    }
}