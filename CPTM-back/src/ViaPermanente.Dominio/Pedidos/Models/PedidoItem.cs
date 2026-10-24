namespace ViaPermanente.Dominio.Pedidos.Models;

/// <summary>PEDIDO_ITEM: comprimento, classe e o padrão de medida do fornecedor; a espécie é opcional.</summary>
public class PedidoItem
{
    public int IdPedidoItem { get; set; }
    public int IdPedido { get; set; }
    public int IdTamanho { get; set; }
    public byte Classe { get; set; }
    public int? IdEspecie { get; set; }
    public int Quantidade { get; set; }
    /// <summary>O padrão de largura × altura do fornecedor (MEDIDA_FORNECEDOR) que vale para o m³ do item.</summary>
    public int IdMedida { get; set; }
}