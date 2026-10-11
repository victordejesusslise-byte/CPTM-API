namespace ViaPermanente.Dominio.Models.Patio;

/// <summary>PILHA: o setor vem do comprimento (A 2,80 · B 3,00 · C 4,00 · D 4,80); a pilha, do setor e da classe.</summary>
public class Pilha
{
    public int IdPilha { get; set; }
    public int IdArmazem { get; set; }
    public string Setor { get; set; } = "";
    public string Codigo { get; set; } = "";
    public int IdTamanho { get; set; }
    public byte Classe { get; set; }
    public bool Ativo { get; set; } = true;
}