using ViaPermanente.Dados;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Ambiental;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Indicadores;
using ViaPermanente.Dados.Pedidos;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Indicadores;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class IndicadorNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Os_18_indicadores_com_o_exemplo()
    {
        var id = (await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto { Nome = "S", Email = "s@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor })).Id;
        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(id);
        var ofertas = new OfertaService(new OfertaRepository(db), new PedidoRepository(db), new CatalogoRepository(db));
        var servico = new IndicadorService(new IndicadorRepository(db), new RelatorioAmbientalRepository(db),
            new PedidoService(new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db), ofertas), new CatalogoRepository(db), new CadastroRepository(db));

        var indicadores = (await servico.Calcular(null, null, DateTime.Now.AddMinutes(1))).ToDictionary(i => i.Chave);

        Assert.Equal(18, indicadores.Count);
        Assert.Equal(7.1m, indicadores["reprovacao-fornecedor"].Valor);             // 2 de 28
        Assert.Equal(2.970m, indicadores["estoque-m3"].Valor);                       // 26 × 0,11424
        Assert.Equal(0m, indicadores["divergencia-nota-chegada"].Valor);
        Assert.Equal(0m, indicadores["segunda-classe-pedido"].Valor);
        Assert.Null(indicadores["disponibilidade-registros"].Valor);
        Assert.Null(indicadores["acerto-camera"].Valor);                             // sem alerta decidido
        Assert.False(indicadores["confiabilidade-dados"].Vermelho);
    }

    [Fact]
    public async Task Com_os_sinais_da_peca_vem_a_nota_das_pecas_em_uso()
    {
        var id = (await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto { Nome = "S", Email = "s@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor })).Id;
        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(id);
        var ofertas = new OfertaService(new OfertaRepository(db), new PedidoRepository(db), new CatalogoRepository(db));
        var servico = new IndicadorService(new IndicadorRepository(db), new RelatorioAmbientalRepository(db),
            new PedidoService(new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db), ofertas), new CatalogoRepository(db), new CadastroRepository(db),
            new SinaisPecaRepository(db));

        var indicadores = (await servico.Calcular(null, null, DateTime.Now.AddMinutes(1))).ToDictionary(i => i.Chave);

        Assert.Equal(19, indicadores.Count);
        var notas = indicadores["notas-das-pecas"];
        Assert.Equal(0m, notas.Valor);                                                // as 26 do estoque foram aprovadas e nada as derrubou
        Assert.False(notas.Vermelho);
        Assert.Equal("0", notas.Detalhes.Single(d => d.Rotulo == "sem avaliação").Valor);
        Assert.Equal(26, notas.Detalhes.Where(d => d.Rotulo != "sem avaliação").Sum(d => int.Parse(d.Valor)));
    }
}