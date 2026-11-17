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

    [Fact]
    public async Task A_passagem_aberta_traz_as_estacoes_os_pontos_do_GPS_e_o_GPS_de_cada_leitura()
    {
        var supervisor = await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Supervisor de teste", Email = "supervisor@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor
        });
        await new DemonstracaoService(new ApresentacaoRepository(db)).Criar(supervisor.Id);
        var servico = new PassagemConsultaService(new PassagemConsultaRepository(db), new ParametroRepository(db), new PecaService(new PecaRepository(db), new CatalogoRepository(db)));

        var detalhe = await servico.Buscar((await servico.Ultimas(30))[0].Id);

        Assert.Equal("DEMO Estação A", detalhe!.EstacaoInicio);
        Assert.Equal("DEMO Estação B", detalhe.EstacaoFim);
        Assert.Equal(10, detalhe.PontosGps);
        Assert.All(detalhe.Leituras, l => { Assert.NotNull(l.Latitude); Assert.NotNull(l.Longitude); });
    }

    [Fact]
    public async Task Passagem_sem_GPS_abre_sem_estacao_e_sem_pontos()
    {
        await new VarreduraService(new VarreduraRepository(db), new ParametroRepository(db)).Registrar(new Dominio.Via.Dtos.NovaPassagemDto
        {
            Codigo = "SEM-GPS", DataInicio = new DateTime(2026, 11, 16, 22, 0, 0),
            Leituras = [new() { Data = new DateTime(2026, 11, 16, 22, 5, 0), KmMedido = 12.3m }]
        });
        db.ChangeTracker.Clear();
        var servico = new PassagemConsultaService(new PassagemConsultaRepository(db), new ParametroRepository(db), new PecaService(new PecaRepository(db), new CatalogoRepository(db)));

        var detalhe = await servico.Buscar((await servico.Ultimas(30))[0].Id);

        Assert.Null(detalhe!.EstacaoInicio);
        Assert.Null(detalhe.EstacaoFim);
        Assert.Equal(0, detalhe.PontosGps);
        Assert.Null(detalhe.Leituras[0].Latitude);
    }
}
