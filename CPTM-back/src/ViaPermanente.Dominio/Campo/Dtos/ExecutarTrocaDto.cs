using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// A via confere as duas peças com o módulo: a que sai (a do pedido, não a vizinha; se não responde, marca)
/// e a que entra (a que o pátio separou). Equipe e foto da peça nova no lugar são obrigatórias.
/// </summary>
public class ExecutarTrocaDto
{
    /// <summary>a leitura da tag da peça que sai (→ LEITURA_DORMENTE); nula só se TagSaiuNaoResponde.</summary>
    public LeituraDto? LeituraSaiu { get; init; }
    /// <summary>verdadeiro quando a tag da peça que sai não responde: a troca vai pelo km e pela foto.</summary>
    public bool TagSaiuNaoResponde { get; init; }
    /// <summary>verdadeiro quando a peça que sai nunca teve tag (Victor, 2026-11-17): não lê a peça e não grava falha; não marca junto com TagSaiuNaoResponde.</summary>
    public bool SaiuNuncaTeveTag { get; init; }
    /// <summary>a leitura da tag da peça que entra (a separada pelo pátio); obrigatória.</summary>
    [Required] public LeituraDto LeituraEntrou { get; init; } = new();
    /// <summary>a equipe de campo que fez a troca (ex.: "Equipe A"); obrigatória.</summary>
    public string Equipe { get; init; } = "";
    /// <summary>o caminho da foto da peça nova no lugar, devolvido pelo envio da foto; obrigatória.</summary>
    public string FotoArquivo { get; init; } = "";
    /// <summary>o km da troca, em km com 3 casas; nulo: fica o km do pedido.</summary>
    public decimal? Km { get; init; }
    /// <summary>Gerado no celular: a troca enviada de novo devolve o estado atual, sem duplicar.</summary>
    public Guid? IdCliente { get; init; }
    /// <summary>A hora do campo (feita sem rede, chega depois). Nula: a hora do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}