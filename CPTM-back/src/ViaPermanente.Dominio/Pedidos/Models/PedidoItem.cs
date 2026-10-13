namespace ViaPermanente.Dominio.Pedidos.Models;

/// <summary>PEDIDO_ITEM: tamanho e classe; a espécie é opcional.</summary>
public class PedidoItem
{
    public int IdPedidoItem { get; set; }
    public int IdPedido { get; set; }
    public int IdTamanho { get; set; }
    public byte Classe { get; set; }
    public int? IdEspecie { get; set; }
    public int Quantidade { get; set; }
}