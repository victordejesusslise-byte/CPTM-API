using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Testes.Services;

/// <summary>Os casos da Tarefa 16 do plano e a conferência dos feixes com o pedido.</summary>
public class ConferenciaChegadaTestes
{
    private static readonly Dictionary<int, TamanhoDormente> Tamanhos = new()
    {
        [1] = new() { IdTamanho = 1, ComprimentoM = 2.80m },
        [2] = new() { IdTamanho = 2, ComprimentoM = 3.00m }
    };
    private static readonly PedidoItem[] Pedido = [new() { IdTamanho = 1, Classe = 1, Quantidade = 28 }];

    [Fact]
    public void Feixes_que_somam_a_nota_nao_geram_divergencia()
        => Assert.Null(ConferenciaChegada.Conferir(28, 28, [14, 14]));

    [Fact]
    public void Informada_diferente_da_nota_gera_divergencia()
    {
        var d = ConferenciaChegada.Conferir(28, 27, [14, 13]);

        Assert.NotNull(d);
        Assert.Equal((28, 27, 27), (d.QuantidadeNota!.Value, d.QuantidadeInformada, d.SomaFeixes));
        Assert.Contains("a nota diz 28, o operador informou 27 e os feixes somam 27", d.Mensagem);
    }

    [Fact]
    public void Sem_nota_confere_so_com_os_feixes()
    {
        Assert.Null(ConferenciaChegada.Conferir(null, 20, [20]));
        Assert.NotNull(ConferenciaChegada.Conferir(null, 20, [14]));
    }

    [Fact]
    public void Feixe_que_nao_foi_pedido_diverge()
        => Assert.Contains("3,00 m de 1ª classe chegou, mas não foi pedido.",
            ConferenciaChegada.ContraPedido(Pedido, [], [(2, 1, 14)], Tamanhos));

    [Fact]
    public void Chegada_acima_do_pedido_diverge()
        => Assert.Contains("2,80 m de 1ª classe: já chegaram 34 peças de 28 pedidas.",
            ConferenciaChegada.ContraPedido(Pedido, [(1, 1, 14)], [(1, 1, 20)], Tamanhos));

    [Fact]
    public void Chegada_dentro_do_pedido_nao_diverge()
        => Assert.Empty(ConferenciaChegada.ContraPedido(Pedido, [(1, 1, 14)], [(1, 1, 14)], Tamanhos));
}