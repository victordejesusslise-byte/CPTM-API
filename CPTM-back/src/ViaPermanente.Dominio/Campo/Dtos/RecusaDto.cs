namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>Uma recusa da via (uma volta da troca ou do lote) e a resposta do técnico.</summary>
/// <param name="Id">id da recusa (→ RECUSA_TROCA).</param>
/// <param name="Data">quando a via recusou (a hora do campo).</param>
/// <param name="IdUsuario">o operador da via que recusou (→ USUARIO).</param>
/// <param name="Motivo">por que a via não trocou.</param>
/// <param name="Descricao">o que a via viu; no "não deu" do lote, começa com "Não deu no lote LT-N".</param>
/// <param name="FotoArquivo">o caminho da foto do local (obrigatória).</param>
/// <param name="Decisao">a resposta do técnico: Manter ou Aceitar; nula enquanto ele não decide.</param>
/// <param name="IdUsuarioDecisao">o técnico que decidiu (→ USUARIO).</param>
/// <param name="DataDecisao">quando o técnico decidiu.</param>
/// <param name="Resposta">o que o técnico respondeu à via (obrigatória para manter).</param>
public record RecusaDto(int Id, DateTime Data, int IdUsuario, string Motivo, string? Descricao, string FotoArquivo,
    string? Decisao, int? IdUsuarioDecisao, DateTime? DataDecisao, string? Resposta);