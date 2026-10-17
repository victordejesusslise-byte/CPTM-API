namespace ViaPermanente.Dominio.Chegadas.Models;

/// <summary>FEIXE: os dados da placa ficam aqui, não copiados em cada peça.</summary>
public class Feixe
{
    public int IdFeixe { get; set; }
    public int IdRecebimento { get; set; }
    public string NumeroFeixe { get; set; } = "";
    public int Quantidade { get; set; }
    public int IdEspecie { get; set; }
    public int IdTamanho { get; set; }
    /// <summary>1ª ou 2ª: vem do item do pedido que o feixe atende (A classe vem do pedido).</summary>
    public byte Classe { get; set; }
    public string? Tratamento { get; set; }
    public int? IdEmpresaTratamento { get; set; }
    public int? IdEmpresaInspecao { get; set; }
    public short? AnoInspecao { get; set; }
}