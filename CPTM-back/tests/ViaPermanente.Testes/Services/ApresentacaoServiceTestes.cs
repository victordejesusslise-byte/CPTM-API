using System.Linq.Expressions;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Apresentacao.Models;

namespace ViaPermanente.Testes.Services;

public class ApresentacaoServiceTestes
{
    private static readonly ContagemFluxo Exemplo = new(
        Pedidos: 1, ItensPedido: 1, PecasPedidas: 28, Notas: 1,
        Recebimentos: 1, Lotes: 1, Feixes: 2, PecasNosFeixes: 28,
        Dormentes: 28, TagsAtivas: 26, TagsDesativadas: 2, Classificacoes: 28,
        Aprovadas: 26, Reprovadas: 2, Movimentacoes: 26, EmPilha: 26);

    [Fact]
    public void Cinco_etapas_na_ordem_do_fluxo()
    {
        var etapas = ApresentacaoService.Etapas(Exemplo);

        Assert.Equal(["Pedido", "Chegada da carga", "Cadastro da peça", "Verificação de qualidade", "Armazém"], etapas.Select(e => e.Nome));
        Assert.All(etapas, e => Assert.True(e.Feita));
        Assert.Contains("26 aprovada(s) e 2 reprovada(s)", etapas[3].Resumo);
        Assert.Equal(26, etapas[4].Linhas);
    }

    [Fact]
    public void Banco_vazio_mostra_as_etapas_por_fazer()
    {
        var vazio = new ContagemFluxo(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        Assert.All(ApresentacaoService.Etapas(vazio), e => Assert.False(e.Feita));
    }

    [Fact]
    public async Task Pedido_que_nao_existe_devolve_nulo()
        => Assert.Null(await new ApresentacaoService(new SoContagem(new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0))).Fluxo(999));

    private class SoContagem(ContagemFluxo contagem) : IApresentacaoRepository
    {
        public Task<ContagemFluxo> Contar(int? idPedido) => Task.FromResult(contagem);
        public Task<T?> Primeiro<T>(Expression<Func<T, bool>> filtro) where T : class => throw new NotImplementedException();
        public Task<T> Adicionar<T>(T entidade) where T : class => throw new NotImplementedException();
        public Task AdicionarVarios<T>(IReadOnlyList<T> entidades) where T : class => throw new NotImplementedException();
        public Task Salvar() => throw new NotImplementedException();
        public Task<string?> UltimaTag() => throw new NotImplementedException();
        public Task<List<TId>> Ids<T, TId>(Expression<Func<T, bool>> filtro, Expression<Func<T, TId>> id) where T : class => throw new NotImplementedException();
        public Task<int> Apagar<T>(Expression<Func<T, bool>> filtro) where T : class => throw new NotImplementedException();
        public Task EmTransacao(Func<Task> acao) => throw new NotImplementedException();
    }
}