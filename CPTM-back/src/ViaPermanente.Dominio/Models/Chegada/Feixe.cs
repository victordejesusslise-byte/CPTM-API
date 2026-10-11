namespace ViaPermanente.Dominio.Models.Chegada;

/// <summary>FEIXE: os dados da placa ficam aqui, não copiados em cada peça.</summary>
public class Feixe
{
    public int IdFeixe { get; set; }
    public int IdRecebimento { get; set; }
    public string NumeroFeixe { get; set; } = "";
    public int Quantidade { get; set; }
    public int IdEspecie { get; set; }
    public int IdTamanho { get; set; }
    public string? Tratamento { get; set; }
    public int? IdEmpresaTratamento { get; set; }
    public int? IdEmpresaInspecao { get; set; }
    public short? AnoInspecao { get; set; }
}