using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Dtos.Pedidos;

/// <summary>O pedido que está sendo montado na tela. Com o fornecedor, a verificação também olha a oferta dele.</summary>
public class VerificarPedidoDto
{
    public int? IdEmpresa { get; init; }
    [Required] public List<NovoPedidoItemDto> Itens { get; init; } = [];
}