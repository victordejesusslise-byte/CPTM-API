namespace ViaPermanente.Dominio.Dtos.Trocas;

/// <summary>A via recusa a troca: motivo, o que viu e foto. A peça separada volta ao armazém.</summary>
public class RecusarTrocaDto
{
    public string Motivo { get; init; } = "";
    public string? Descricao { get; init; }
    public string FotoArquivo { get; init; } = "";
}