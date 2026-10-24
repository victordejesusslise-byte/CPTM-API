namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>A via recusa a troca: motivo, o que viu e foto. A peça separada volta ao armazém.</summary>
public class RecusarTrocaDto
{
    public string Motivo { get; init; } = "";
    public string? Descricao { get; init; }
    public string FotoArquivo { get; init; } = "";
    /// <summary>Gerado no celular: a recusa enviada de novo devolve o estado atual, sem duplicar.</summary>
    public Guid? IdCliente { get; init; }
    /// <summary>A hora do campo (feita sem rede, chega depois). Nula: a hora do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}