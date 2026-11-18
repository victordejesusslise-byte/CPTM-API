using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// Um dormente trocado no lote: a tag da peça que sai (ou, se ela não responde, a peça escolhida pelo km na faixa), a tag da peça que entra
/// (uma das separadas) e a foto da peça nova no lugar. Uma foto por dormente (Victor, 2026-11-11).
/// </summary>
public class ParDoLoteDto
{
    /// <summary>a leitura da tag da peça que sai; nula só se TagSaiuNaoResponde.</summary>
    public LeituraDto? LeituraSaiu { get; init; }
    /// <summary>verdadeiro quando a tag da peça que sai não responde: aí vale IdDormenteSaiu.</summary>
    public bool TagSaiuNaoResponde { get; init; }
    /// <summary>Só com a tag da peça que sai sem responder: a peça escolhida na lista da faixa.</summary>
    public int? IdDormenteSaiu { get; init; }
    /// <summary>
    /// verdadeiro quando o dormente que sai é antigo e nunca teve tag nem cadastro (Victor, 2026-11-17): aí o Km é obrigatório e tem que estar
    /// na faixa do lote; a troca anota só o km. Não marca junto com TagSaiuNaoResponde.
    /// </summary>
    public bool SaiuNuncaTeveTag { get; init; }
    /// <summary>a leitura da tag da peça que entra (uma das separadas); obrigatória.</summary>
    [Required] public LeituraDto LeituraEntrou { get; init; } = new();
    /// <summary>o caminho da foto da peça nova no lugar (uma por dormente); obrigatória.</summary>
    public string FotoArquivo { get; init; } = "";
    /// <summary>o km deste dormente, em km com 3 casas; nulo: o km da peça que sai.</summary>
    public decimal? Km { get; init; }
    /// <summary>A hora do campo deste dormente. Nula: a do fechamento.</summary>
    public DateTime? RegistradoEm { get; init; }
}

/// <summary>A via fecha o lote: os dormentes trocados e a equipe. Conferido ID por ID; o lote mandado duas vezes pelo celular entra uma vez.</summary>
public record FecharLoteTrocaDto
{
    /// <summary>os dormentes trocados, na ordem em que a via trocou; pelo menos um.</summary>
    public List<ParDoLoteDto> Pares { get; init; } = [];
    /// <summary>As obrigatórias (trocas avulsas já aprovadas na faixa) que a via não conseguiu trocar, com motivo e foto.</summary>
    public List<NaoDeuDto> NaoDeu { get; init; } = [];
    /// <summary>a equipe de campo que trocou o lote (ex.: "Equipe A"); obrigatória.</summary>
    public string Equipe { get; init; } = "";
    /// <summary>gerado no celular: o lote mandado duas vezes entra uma vez só.</summary>
    public Guid? IdCliente { get; init; }
    /// <summary>a hora do campo em que a via fechou o lote; nula: a hora do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}

/// <summary>
/// A via não conseguiu trocar uma obrigatória do lote (Victor, 2026-11-12): motivo e foto. A troca avulsa dela volta para o técnico como
/// recusa, como a recusa da troca de hoje (ele mantém ou aceita).
/// </summary>
public class NaoDeuDto
{
    /// <summary>id da troca avulsa obrigatória que não deu para trocar (→ TROCA).</summary>
    public int IdTroca { get; init; }
    /// <summary>por que não deu; obrigatório.</summary>
    public string Motivo { get; init; } = "";
    /// <summary>o que a via viu; vai na recusa depois de "Não deu no lote LT-N".</summary>
    public string? Descricao { get; init; }
    /// <summary>o caminho da foto do local; obrigatória.</summary>
    public string FotoArquivo { get; init; } = "";
    /// <summary>a hora do campo deste "não deu"; nula: a hora do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}
