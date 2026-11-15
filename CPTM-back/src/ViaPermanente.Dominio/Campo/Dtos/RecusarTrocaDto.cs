namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>A via recusa a troca: motivo, o que viu e foto. A peça separada volta ao armazém.</summary>
public class RecusarTrocaDto
{
    /// <summary>por que a via não trocou; obrigatório.</summary>
    public string Motivo { get; init; } = "";
    /// <summary>o que a via viu no local.</summary>
    public string? Descricao { get; init; }
    /// <summary>o caminho da foto do local, devolvido pelo envio da foto; obrigatória.</summary>
    public string FotoArquivo { get; init; } = "";
    /// <summary>Gerado no celular: a recusa enviada de novo devolve o estado atual, sem duplicar.</summary>
    public Guid? IdCliente { get; init; }
    /// <summary>A hora do campo (feita sem rede, chega depois). Nula: a hora do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}