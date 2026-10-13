using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>A nota única no SQL Server, com o banco de demonstração: a peça com alerta aberto cai para Ruim, a reprovada é Crítico.</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class NotaDaPecaNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();

    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Cada_peca_ganha_a_nota_da_avaliacao_mais_recente()
    {
        var supervisor = await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Supervisor de teste", Email = "supervisor@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor
        });
        await new DemonstracaoService(new ApresentacaoRepository(db)).Criar(supervisor.Id);
        var pecas = new PecaService(new PecaRepository(db), new CatalogoRepository(db), new SinaisPecaRepository(db));

        var naVia = await db.Set<Dormente>().Where(d => d.IdTrecho != null).OrderBy(d => d.KmProjeto).Select(d => d.IdDormente).ToListAsync();
        var reprovada = await db.Set<VerificacaoQualidade>().Where(v => v.Resultado == ResultadoVerificacao.Reprovada).Select(v => v.IdDormente).FirstAsync();
        var todas = await pecas.PorIds([.. naVia, reprovada]);

        Assert.Equal("Ruim", todas[naVia[0]].Nota);   // fora de posição, alerta aberto
        Assert.Equal("Ruim", todas[naVia[1]].Nota);   // tag muda, confirmado
        Assert.Equal("Bom", todas[naVia[2]].Nota);    // a rachadura foi descartada: vale a verificação aprovada
        Assert.Equal("Crítico", todas[reprovada].Nota);
        Assert.Equal("NaVia", todas[naVia[0]].Situacao);
        Assert.All(await pecas.Estoque(), p => Assert.NotNull(p.Nota));
    }
}
