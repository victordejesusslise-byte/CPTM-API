using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Chegadas;

/// <summary>
/// A chegada não é aceita em silêncio: o operador informa quantas peças chegaram, e o sistema confere
/// com a nota e com a soma dos feixes, e os feixes com o pedido.
/// </summary>
public static class ConferenciaChegada
{
    /// <summary>Nulo quando nota (se houver), informada e feixes batem; senão, a divergência com os três números.</summary>
    public static DivergenciaChegada? Conferir(int? quantidadeNota, int quantidadeInformada, IReadOnlyList<int> pecasPorFeixe)
    {
        var soma = pecasPorFeixe.Sum();
        if ((quantidadeNota is null || quantidadeNota == quantidadeInformada) && soma == quantidadeInformada)
            return null;
        var nota = quantidadeNota is int n ? $"a nota diz {n}, " : "";
        return new DivergenciaChegada(quantidadeNota, quantidadeInformada, soma,
            $"A quantidade não bate: {nota}o operador informou {quantidadeInformada} e os feixes somam {soma}.");
    }

    /// <summary>
    /// Os feixes desta chegada contra o pedido, por tamanho e classe (a classe vem da espécie da plaquinha):
    /// o que não foi pedido e o que, somado às chegadas anteriores, passa do pedido.
    /// </summary>
    public static IReadOnlyList<string> ContraPedido(
        IReadOnlyList<PedidoItem> pedido,
        IReadOnlyList<(int IdTamanho, byte Classe, int Quantidade)> chegadasAnteriores,
        IReadOnlyList<(int IdTamanho, byte Classe, int Quantidade)> estaChegada,
        IReadOnlyDictionary<int, TamanhoDormente> tamanhos)
    {
        var divergencias = new List<string>();
        foreach (var grupo in estaChegada.GroupBy(f => (f.IdTamanho, f.Classe)))
        {
            var rotulo = $"{FormatoHelper.Metros(tamanhos[grupo.Key.IdTamanho].ComprimentoM)} de {grupo.Key.Classe}ª classe";
            var pedidas = pedido.Where(p => p.IdTamanho == grupo.Key.IdTamanho && p.Classe == grupo.Key.Classe).Sum(p => p.Quantidade);
            var chegaram = chegadasAnteriores.Where(c => c.IdTamanho == grupo.Key.IdTamanho && c.Classe == grupo.Key.Classe).Sum(c => c.Quantidade)
                           + grupo.Sum(f => f.Quantidade);
            if (pedidas == 0)
                divergencias.Add($"{rotulo} chegou, mas não foi pedido.");
            else if (chegaram > pedidas)
                divergencias.Add($"{rotulo}: já chegaram {chegaram} peças de {pedidas} pedidas.");
        }
        return divergencias;
    }
}