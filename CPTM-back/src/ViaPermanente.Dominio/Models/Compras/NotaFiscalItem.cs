namespace ViaPermanente.Dominio.Models.Compras;

/// <summary>NOTA_FISCAL_ITEM: o volume é o declarado na nota.</summary>
public class NotaFiscalItem
{
    public int IdNotaItem { get; set; }
    public int IdNota { get; set; }
    public int IdEspecie { get; set; }
    public int IdTamanho { get; set; }
    public byte Classe { get; set; }
    public int Quantidade { get; set; }
    public decimal VolumeM3 { get; set; }
}