using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Pedidos.Dtos;

/// <summary>Um item do pedido: tamanho e classe; a espécie é opcional e, se vier, define a classe.</summary>
public class NovoPedidoItemDto
{
    [Range(1, int.MaxValue)] public int IdTamanho { get; init; }
    [Range(1, 2)] public byte Classe { get; init; }
    public int? IdEspecie { get; init; }
    [Range(1, int.MaxValue)] public int Quantidade { get; init; }
}