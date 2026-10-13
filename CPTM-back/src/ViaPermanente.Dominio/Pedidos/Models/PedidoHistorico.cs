namespace ViaPermanente.Dominio.Pedidos.Models;

/// <summary>PEDIDO_HISTORICO: só insere; os retratos são JSON.</summary>
public class PedidoHistorico
{
    public int IdHistorico { get; set; }
    public int IdPedido { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Data { get; set; }
    public string RetratoAntes { get; set; } = "";
    public string RetratoDepois { get; set; } = "";
    public string Motivo { get; set; } = "";
}