namespace ViaPermanente.Dominio.Models.Patio;

/// <summary>LEITOR: um cadastro só para todo leitor de tag (pistola, módulo do celular, carro).</summary>
public class Leitor
{
    public int IdLeitor { get; set; }
    public int IdTipoLeitor { get; set; }
    public int? IdSetor { get; set; }
    public string Codigo { get; set; } = "";
    public string? NumeroSerie { get; set; }
    public string? VersaoHardware { get; set; }
    public string? VersaoFirmware { get; set; }
    public bool Ativo { get; set; } = true;
}