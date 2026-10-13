using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Pedidos.Dtos;

/// <summary>O que a tela manda para registrar um pedido.</summary>
public class NovoPedidoDto
{
    [Required] public string Codigo { get; init; } = "";
    [Range(1, int.MaxValue)] public int IdEmpresa { get; init; }
    public int? IdContrato { get; init; }
    /// <summary>Sem data, vale a de hoje.</summary>
    public DateOnly? DataCompra { get; init; }
    [Required] public List<NovoPedidoItemDto> Itens { get; init; } = [];
}