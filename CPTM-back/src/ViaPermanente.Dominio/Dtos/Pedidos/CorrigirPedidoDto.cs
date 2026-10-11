using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Dtos.Pedidos;

/// <summary>
/// A correção do pedido, só antes da primeira chegada (Pedido trava quando a carga chega), sempre com motivo.
/// Os itens vêm completos: substituem os de antes. A regra da 2ª classe é conferida de novo.
/// </summary>
public class CorrigirPedidoDto
{
    public int? IdContrato { get; init; }
    public DateOnly? DataCompra { get; init; }
    [Required] public List<NovoPedidoItemDto> Itens { get; init; } = [];
    [Required] public string Motivo { get; init; } = "";
}