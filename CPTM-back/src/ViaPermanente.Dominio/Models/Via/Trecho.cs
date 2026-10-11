namespace ViaPermanente.Dominio.Models.Via;

/// <summary>TRECHO: entre duas estações vizinhas; o par é único.</summary>
public class Trecho
{
    public int IdTrecho { get; set; }
    public int IdEstacaoInicio { get; set; }
    public int IdEstacaoFim { get; set; }
    public byte ClasseExigida { get; set; }
}