
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>LOTE: a madeira de um pedido que chegou ao pátio. Nasce na primeira chegada do pedido, e as chegadas
/// seguintes usam o mesmo lote (decisão: O lote nasce da chegada).</summary>
public class Lote
{
    /// <summary>Id do lote; é o L01 do código da peça (DM-E01-L01-0028).</summary>
    public int IdLote { get; set; }
    /// <summary>Id da empresa fornecedora (→ EMPRESA); é o E01 do código da peça.</summary>
    public int IdEmpresa { get; set; }
    /// <summary>Id do pedido de onde veio a madeira (→ PEDIDO).</summary>
    public int IdPedido { get; set; }
    /// <summary>O código do lote: L- e o código do pedido.</summary>
    public string Codigo { get; set; } = "";
    /// <summary>Quando o lote foi criado.</summary>
    public DateTime DataCriacao { get; set; }
    /// <summary>Texto livre.</summary>
    public string? Descricao { get; set; }
}