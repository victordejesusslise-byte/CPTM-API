namespace ViaPermanente.Dominio.Ambiental.Models;

/// <summary>RELATORIO_AMBIENTAL_ITEM: estoque por espécie em m³ e tempo.</summary>
public class RelatorioAmbientalItem
{
    public int IdItem { get; set; }
    public int IdRelatorio { get; set; }
    public int IdEspecie { get; set; }
    public decimal VolumeM3Estoque { get; set; }
    public decimal VolumeM3Entrada { get; set; }
    public decimal VolumeM3Saida { get; set; }
    public int IdadeMediaDias { get; set; }
}