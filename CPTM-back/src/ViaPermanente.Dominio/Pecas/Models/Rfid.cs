
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>RFID: a tag UHF da peça. Cada peça tem a própria tag; só uma ativa por peça.</summary>
public class Rfid
{
    public int IdRfid { get; set; }
    public int IdDormente { get; set; }
    public string TagRfid { get; set; } = "";
    public bool Ativo { get; set; } = true;
    public DateTime? VinculadaEm { get; set; }
    public int? IdUsuarioVinculou { get; set; }
}