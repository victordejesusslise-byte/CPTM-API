
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>LEITOR: um cadastro só para todo leitor de tag (pistola, módulo do celular, carro).</summary>
public class Leitor
{
    public int IdLeitor { get; set; }
    /// <summary>O tipo, pelo número fixo no código: 1 Módulo RFID, 2 Pistola NIR, 3 Carro.</summary>
    public TipoLeitor Tipo { get; set; }
    public int? IdSetor { get; set; }
    public string Codigo { get; set; } = "";
    public string? NumeroSerie { get; set; }
    public string? VersaoHardware { get; set; }
    public string? VersaoFirmware { get; set; }
    public bool Ativo { get; set; } = true;
}