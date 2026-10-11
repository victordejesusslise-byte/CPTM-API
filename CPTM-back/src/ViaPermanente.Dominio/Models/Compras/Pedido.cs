namespace ViaPermanente.Dominio.Models.Compras;

/// <summary>PEDIDO: a 2ª classe fica em até 20%, na unidade de PARAMETRO.</summary>
public class Pedido
{
    public int IdPedido { get; set; }
    public int IdEmpresa { get; set; }
    public int IdUsuario { get; set; }
    public int? IdContrato { get; set; }
    public string Codigo { get; set; } = "";
    public DateOnly DataCompra { get; set; }
    public int Quantidade { get; set; }
    public DateTime CriadoEm { get; set; }
    public StatusPedido Status { get; set; }
}