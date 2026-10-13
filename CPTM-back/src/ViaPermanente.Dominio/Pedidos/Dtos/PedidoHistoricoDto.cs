namespace ViaPermanente.Dominio.Pedidos.Dtos;

/// <summary>Uma correção ou um cancelamento do pedido: quem, quando, por quê, e o retrato (JSON) de antes e de depois.</summary>
public record PedidoHistoricoDto(int Id, DateTime Data, int IdUsuario, string Motivo, string RetratoAntes, string RetratoDepois);