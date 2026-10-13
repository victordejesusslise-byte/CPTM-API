namespace ViaPermanente.Dominio.Pedidos.Models;

/// <summary>OFERTA_FORNECEDOR: m³ disponível por espécie e tamanho.</summary>
public class OfertaFornecedor
{
    public int IdOferta { get; set; }
    public int IdEmpresa { get; set; }
    public int IdEspecie { get; set; }
    public int IdTamanho { get; set; }
    public decimal VolumeM3Disponivel { get; set; }
    public DateOnly Data { get; set; }
}