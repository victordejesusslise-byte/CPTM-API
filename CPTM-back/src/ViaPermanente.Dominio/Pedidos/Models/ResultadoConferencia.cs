namespace ViaPermanente.Dominio.Pedidos.Models;

/// <summary>A conferência da nota com o pedido. Divergência muda o status; aviso só informa.</summary>
public record ResultadoConferencia(bool Confere, IReadOnlyList<string> Divergencias, IReadOnlyList<string> Avisos);