namespace ViaPermanente.Dominio.Via.Models;

/// <summary>PASSAGEM: uma linha por noite do carro de inspeção.</summary>
public class Passagem
{
    public int IdPassagem { get; set; }
    public string Codigo { get; set; } = "";
    public string? Veiculo { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public decimal? KmInicio { get; set; }
    public decimal? KmFim { get; set; }
}