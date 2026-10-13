using ViaPermanente.Dados;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dados.Via;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Dominio.Via;

namespace ViaPermanente.Testes.Banco;

/// <summary>A consulta das passagens no SQL Server, com a passagem da demonstração (3 leituras, 3 alertas de sensor).</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class PassagemConsultaNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();

    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Lista_a_passagem_com_o_resumo_e_abre_com_as_leituras_com_problema_primeiro()
    {
        var supervisor = await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Supervisor de teste", Email = "supervisor@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor
        });
        await new DemonstracaoService(new ApresentacaoRepository(db)).Criar(supervisor.Id);
        var servico = new PassagemConsultaService(new PassagemConsultaRepository(db), new ParametroRepository(db), new PecaService(new PecaRepository(db), new CatalogoRepository(db)));

        var lista = await servico.Ultimas(30);
        var resumo = Assert.Single(lista);
        Assert.Equal(3, resumo.Leituras);
        Assert.Equal(1, resumo.SemResposta);
        Assert.Equal(1, resumo.ForaDePosicao);
        Assert.Equal(3, resumo.AlertasSensor);

        var detalhe = await servico.Buscar(resumo.Id);
        Assert.Equal(3, detalhe!.Leituras.Count);
        Assert.All(detalhe.Leituras, l => Assert.StartsWith("DM-", l.CodigoPeca));
        Assert.True(!detalhe.Leituras[0].Leitura || detalhe.Leituras[0].ForaDePosicao || detalhe.Leituras[0].RachaduraSuspeita);
        Assert.Null(await servico.Buscar(999999));
    }
}
