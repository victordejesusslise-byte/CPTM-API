using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Pedidos.Dtos;

/// <summary>Um item do pedido: comprimento, classe e o padrão de medida do fornecedor; a espécie é opcional.</summary>
public class NovoPedidoItemDto
{
    [Range(1, int.MaxValue)] public int IdTamanho { get; init; }
    [Range(1, 2)] public byte Classe { get; init; }
    public int? IdEspecie { get; init; }
    [Range(1, int.MaxValue)] public int Quantidade { get; init; }
    /// <summary>O padrão de medida (largura × altura) do fornecedor, escolhido para o item.</summary>
    [Range(1, int.MaxValue)] public int IdMedida { get; init; }
}