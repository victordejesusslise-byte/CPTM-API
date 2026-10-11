namespace ViaPermanente.Dominio.Dtos.Armazem;

/// <summary>A leitura de uma pilha na conferência, no modo Armazém: todas as tags que responderam ali.</summary>
public class LerPilhaDto
{
    public int IdPilha { get; init; }
    public List<string> Tags { get; init; } = [];
    public int? IdLeitor { get; init; }
    public int? IdModo { get; init; }
}