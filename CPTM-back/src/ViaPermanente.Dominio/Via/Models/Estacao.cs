namespace ViaPermanente.Dominio.Via.Models;

/// <summary>ESTACAO: as estações da linha, em ordem.</summary>
public class Estacao
{
    public int IdEstacao { get; set; }
    public string Nome { get; set; } = "";
    public string Linha { get; set; } = "";
    public decimal Km { get; set; }
    public int Ordem { get; set; }
}