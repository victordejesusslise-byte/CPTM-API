namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>A troca com os quatro passos (quem e quando) e os dias de peça condenada na via desde o pedido.</summary>
/// <param name="Id">id da troca (→ TROCA); nos laudos, LB-0001 e LD-0001.</param>
/// <param name="Status">Pedida, Aprovada, AguardandoPeca, Separada, Feita, Reprovada, Recusada ou Cancelada.</param>
/// <param name="Origem">Inspecao (inspeção reprovada), Requisicao (pedido com motivo) ou Lote (feita num lote de troca).</param>
/// <param name="IdDormenteSaiu">id da peça que sai da via (→ DORMENTE).</param>
/// <param name="CodigoSaiu">código impresso da peça que sai: DM-E(empresa)-L(lote)-(id da peça), ex.: DM-E01-L01-0028.</param>
/// <param name="TagSaiu">a tag UHF ativa da peça que sai.</param>
/// <param name="IdDormenteEntrou">id da peça que entra (→ DORMENTE); nulo antes da separação.</param>
/// <param name="CodigoEntrou">código impresso da peça que entra.</param>
/// <param name="TagEntrou">a tag da peça que entra (a ativa, ou a gravada na troca feita).</param>
/// <param name="IdInspecao">a inspeção reprovada que pediu a troca (→ INSPECAO); nula na requisição e no lote.</param>
/// <param name="IdTrecho">o trecho da troca (→ TRECHO); dá a classe exigida.</param>
/// <param name="Km">o km da troca, em km com 3 casas.</param>
/// <param name="Motivo">por que trocar (obrigatório na requisição).</param>
/// <param name="Observacao">a observação do pedido.</param>
/// <param name="IdUsuarioPediu">o técnico que pediu (→ USUARIO).</param>
/// <param name="DataPedido">quando a troca foi pedida.</param>
/// <param name="IdUsuarioAprovou">quem aprovou ou reprovou (→ USUARIO).</param>
/// <param name="DataAprovacao">quando foi aprovada ou reprovada.</param>
/// <param name="MotivoReprovacao">por que foi reprovada; nulo se não foi.</param>
/// <param name="IdUsuarioSeparou">o operador de pátio que separou a peça (→ USUARIO).</param>
/// <param name="DataSeparacao">quando a peça foi separada.</param>
/// <param name="IdUsuarioFez">o operador da via que fez a troca (→ USUARIO).</param>
/// <param name="Data">quando a troca foi feita (a hora do campo).</param>
/// <param name="Equipe">a equipe de campo que fez a troca.</param>
/// <param name="FotoArquivo">o caminho da foto da peça nova no lugar.</param>
/// <param name="Cancelamento">por que foi cancelada (ex.: "Feita no lote LT-0003"); nulo se não foi.</param>
/// <param name="DiasDesdeOPedido">dias desde o pedido até a troca (ou até hoje).</param>
/// <param name="UltimaRecusa">a última recusa da via (→ RECUSA_TROCA); nula se não houve.</param>
/// <param name="NoiteDoPlano">a noite em que a troca será feita.</param>
/// <param name="Urgente">aprovada depois do corte para entrar na mesma noite.</param>
/// <param name="IdLoteTroca">o lote de troca em que esta troca foi feita (→ LOTE_TROCA); nulo na troca avulsa.</param>
public record TrocaDto(int Id, string Status, string Origem,
    int IdDormenteSaiu, string? CodigoSaiu, string? TagSaiu, int? IdDormenteEntrou, string? CodigoEntrou, string? TagEntrou,
    int? IdInspecao, int? IdTrecho, decimal? Km, string? Motivo, string? Observacao,
    int IdUsuarioPediu, DateTime DataPedido, int? IdUsuarioAprovou, DateTime? DataAprovacao, string? MotivoReprovacao,
    int? IdUsuarioSeparou, DateTime? DataSeparacao, int? IdUsuarioFez, DateTime? Data, string? Equipe, string? FotoArquivo,
    string? Cancelamento, int DiasDesdeOPedido, RecusaDto? UltimaRecusa, DateOnly? NoiteDoPlano, bool Urgente, int? IdLoteTroca = null);