using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// Um dormente trocado no lote: a tag da peça que sai (ou, se ela não responde, a peça escolhida pelo km na faixa), a tag da peça que entra
/// (uma das separadas) e a foto da peça nova no lugar. Uma foto por dormente (Victor, 2026-11-11).
/// </summary>
public class ParDoLoteDto
{
    public LeituraDto? LeituraSaiu { get; init; }
    public bool TagSaiuNaoResponde { get; init; }
    /// <summary>Só com a tag da peça que sai sem responder: a peça escolhida na lista da faixa.</summary>
    public int? IdDormenteSaiu { get; init; }
    [Required] public LeituraDto LeituraEntrou { get; init; } = new();
    public string FotoArquivo { get; init; } = "";
    public decimal? Km { get; init; }
    /// <summary>A hora do campo deste dormente. Nula: a do fechamento.</summary>
    public DateTime? RegistradoEm { get; init; }
}

/// <summary>A via fecha o lote: os dormentes trocados e a equipe. Conferido ID por ID; o lote mandado duas vezes pelo celular entra uma vez.</summary>
public record FecharLoteTrocaDto
{
    public List<ParDoLoteDto> Pares { get; init; } = [];
    /// <summary>As obrigatórias (trocas avulsas já aprovadas na faixa) que a via não conseguiu trocar, com motivo e foto.</summary>
    public List<NaoDeuDto> NaoDeu { get; init; } = [];
    public string Equipe { get; init; } = "";
    public Guid? IdCliente { get; init; }
    public DateTime? RegistradoEm { get; init; }
}

/// <summary>
/// A via não conseguiu trocar uma obrigatória do lote (Victor, 2026-11-12): motivo e foto. A troca avulsa dela volta para o técnico como
/// recusa, como a recusa da troca de hoje (ele mantém ou aceita).
/// </summary>
public class NaoDeuDto
{
    public int IdTroca { get; init; }
    public string Motivo { get; init; } = "";
    public string? Descricao { get; init; }
    public string FotoArquivo { get; init; } = "";
    public DateTime? RegistradoEm { get; init; }
}
