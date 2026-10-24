using System.Globalization;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Pedidos;

/// <summary>O padrão de medida que vale para o que chega: o do item do pedido que atende o comprimento, a classe e a espécie.</summary>
public static class MedidasDoPedido
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>O item do mesmo comprimento e classe: o da mesma espécie, senão o sem espécie, senão o primeiro. Nulo se não há.</summary>
    public static PedidoItem? ItemQueAtende(IReadOnlyList<PedidoItem> pedido, int idTamanho, byte classe, int idEspecie)
    {
        var mesmos = pedido.Where(p => p.IdTamanho == idTamanho && p.Classe == classe).ToList();
        return mesmos.FirstOrDefault(p => p.IdEspecie == idEspecie) ?? mesmos.FirstOrDefault(p => p.IdEspecie is null) ?? mesmos.FirstOrDefault();
    }

    /// <summary>O padrão do item que atende; nulo se nenhum item atende.</summary>
    public static MedidaFornecedor? Do(IReadOnlyList<PedidoItem> pedido, IReadOnlyDictionary<int, MedidaFornecedor> medidas, int idTamanho, byte classe, int idEspecie)
        => ItemQueAtende(pedido, idTamanho, classe, idEspecie) is { } item && medidas.TryGetValue(item.IdMedida, out var medida) ? medida : null;

    /// <summary>"0,24 × 0,17 m".</summary>
    public static string Rotulo(MedidaFornecedor medida) => $"{medida.LarguraM.ToString("0.00", Pt)} × {medida.AlturaM.ToString("0.00", Pt)} m";
}
