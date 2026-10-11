namespace ViaPermanente.Dominio.Models.Patio;

/// <summary>ARMAZEM: onde a carga chega e as peças ficam em pilhas.</summary>
public class Armazem
{
    public int IdArmazem { get; set; }
    public string Nome { get; set; } = "";
    public string Linha { get; set; } = "";
    public decimal? Km { get; set; }
    public string? Endereco { get; set; }
    public bool Ativo { get; set; } = true;
}