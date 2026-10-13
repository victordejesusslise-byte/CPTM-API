namespace ViaPermanente.Dominio.Pedidos.Dtos;

/// <summary>
/// O pedido como sai da API. Situacao é calculada: Cancelado, Recebido ou RecebidoParcial saem das chegadas
/// (RECEBIMENTO), não de uma coluna guardada; sem chegada, vale o status do pedido.
/// </summary>
public record PedidoDto(
    int Id, string Codigo, int IdEmpresa, string Empresa, int? IdContrato, string? Contrato,
    DateOnly DataCompra, DateTime CriadoEm, int Quantidade, decimal VolumeM3,
    string Status, string Situacao, int PecasRecebidas, LimiteDto SegundaClasse, IReadOnlyList<PedidoItemDto> Itens);