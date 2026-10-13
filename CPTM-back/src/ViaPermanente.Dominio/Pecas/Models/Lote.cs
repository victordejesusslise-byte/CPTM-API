
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>LOTE: nasce da primeira chegada do pedido.</summary>
public class Lote
{
    public int IdLote { get; set; }
    public int IdEmpresa { get; set; }
    public int IdPedido { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime DataCriacao { get; set; }
    public string? Descricao { get; set; }
}