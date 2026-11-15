namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>Uma peça do lote: separada para ele, devolvida dele ou na via dentro da faixa.</summary>
/// <param name="Id">id da peça (→ DORMENTE).</param>
/// <param name="Codigo">código impresso da peça: DM-E(empresa)-L(lote)-(id da peça), ex.: DM-E01-L01-0028.</param>
/// <param name="Tag">a tag UHF ativa da peça; nula sem tag ativa.</param>
/// <param name="KmProjeto">o km da peça na via, em km com 3 casas; nulo fora da via.</param>
/// <param name="IdTamanho">o comprimento da peça (→ TAMANHO_DORMENTE).</param>
/// <param name="Pilha">a pilha onde a peça está; nula fora da pilha.</param>
public record PecaDoLoteDto(int Id, string Codigo, string? Tag, decimal? KmProjeto, int IdTamanho, string? Pilha);

/// <summary>
/// Uma troca avulsa já aprovada (aprovada, aguardando peça ou separada) de uma peça na via, dentro da faixa e do comprimento do lote: a via
/// tem de trocá-la no lote, ou dizer que não deu (Victor, 2026-11-12). Não gasta a quantidade aprovada: soma a ela. Situacao: Aberta (a
/// trocar), Trocada ou NaoDeu (com o motivo). Separada: a peça que o pátio já separou para o avulso, que entra no lugar dela.
/// </summary>
/// <param name="IdTroca">id da troca avulsa obrigatória (→ TROCA).</param>
/// <param name="Situacao">Aberta (a trocar), Trocada (feita no lote) ou NaoDeu (a via disse que não deu).</param>
/// <param name="Peca">a peça da via que tem de sair.</param>
/// <param name="Separada">a peça que o pátio já separou para o avulso; nula se ainda não há.</param>
/// <param name="Motivo">no NaoDeu, o motivo que a via deu; nulo nos outros.</param>
public record ObrigatoriaDto(int IdTroca, string Situacao, PecaDoLoteDto Peca, PecaDoLoteDto? Separada, string? Motivo);

/// <summary>
/// O lote de troca com os passos (quem e quando), as peças separadas e devolvidas e as trocas feitas nele. Aberto (BuscarPorId e o plano
/// da noite), traz também as peças na via dentro da faixa (para o celular conferir sem rede) e as que têm pedido avulso aberto.
/// </summary>
/// <param name="Id">id do lote (→ LOTE_TROCA).</param>
/// <param name="Codigo">o código do lote na tela: LT- e o id com 4 dígitos, ex.: LT-0003.</param>
/// <param name="Status">Pedido, Aprovado, AguardandoPeca, Separado, Feito, Reprovado, Recusado ou Cancelado.</param>
/// <param name="IdTrecho">o trecho do lote (→ TRECHO).</param>
/// <param name="Trecho">o nome do trecho: "estação de início – estação do fim".</param>
/// <param name="ClasseExigida">a classe que o trecho exige: 1 ou 2.</param>
/// <param name="KmInicio">o começo da faixa do lote, em km com 3 casas.</param>
/// <param name="KmFim">o fim da faixa do lote, em km com 3 casas.</param>
/// <param name="IdTamanho">o comprimento das peças do lote (→ TAMANHO_DORMENTE).</param>
/// <param name="ComprimentoM">esse comprimento, em m.</param>
/// <param name="QuantidadePedida">quantas peças o técnico pediu.</param>
/// <param name="QuantidadeAprovada">quantas peças foram aprovadas; nula antes da aprovação.</param>
/// <param name="Motivo">por que o técnico pediu o lote.</param>
/// <param name="Observacao">a observação do pedido.</param>
/// <param name="IdUsuarioPediu">o técnico que pediu (→ USUARIO).</param>
/// <param name="DataPedido">quando o lote foi pedido.</param>
/// <param name="IdUsuarioAprovou">quem aprovou ou reprovou (→ USUARIO); nulo antes da decisão.</param>
/// <param name="DataAprovacao">quando foi aprovado ou reprovado.</param>
/// <param name="MotivoReprovacao">por que foi reprovado; nulo se não foi.</param>
/// <param name="MotivoMenos">por que aprovou menos peças que as pedidas; nulo se aprovou todas.</param>
/// <param name="NoiteDoPlano">a noite em que o lote será trocado.</param>
/// <param name="Urgente">aprovado depois do corte para entrar na mesma noite.</param>
/// <param name="IdUsuarioFez">o operador da via que fechou o lote (→ USUARIO).</param>
/// <param name="Data">quando a via fechou o lote (a hora do campo).</param>
/// <param name="Equipe">a equipe de campo que trocou.</param>
/// <param name="Cancelamento">por que o lote foi cancelado; nulo se não foi.</param>
/// <param name="Separadas">as peças separadas para o lote que ainda não entraram na via.</param>
/// <param name="Devolvidas">as peças separadas que voltaram ao armazém (sobras ou recusa aceita).</param>
/// <param name="Trocas">as trocas feitas no lote, uma por dormente trocado (→ TROCA com origem Lote).</param>
/// <param name="PecasDaFaixa">as peças na via dentro da faixa (só no lote aberto).</param>
/// <param name="AvulsasAbertas">as peças da faixa que já têm troca avulsa aberta (só no lote aberto).</param>
/// <param name="UltimaRecusa">a última recusa do lote pela via (→ RECUSA_TROCA); nula se não houve.</param>
/// <param name="Obrigatorias">as trocas avulsas já aprovadas na faixa, que a via tem de fazer neste lote.</param>
public record LoteTrocaDto(int Id, string Codigo, string Status, int IdTrecho, string Trecho, byte ClasseExigida,
    decimal KmInicio, decimal KmFim, int IdTamanho, decimal ComprimentoM, int QuantidadePedida, int? QuantidadeAprovada,
    string Motivo, string? Observacao, int IdUsuarioPediu, DateTime DataPedido, int? IdUsuarioAprovou, DateTime? DataAprovacao,
    string? MotivoReprovacao, string? MotivoMenos, DateOnly? NoiteDoPlano, bool Urgente, int? IdUsuarioFez, DateTime? Data,
    string? Equipe, string? Cancelamento,
    IReadOnlyList<PecaDoLoteDto> Separadas, IReadOnlyList<PecaDoLoteDto> Devolvidas, IReadOnlyList<TrocaDto> Trocas,
    IReadOnlyList<PecaDoLoteDto> PecasDaFaixa, IReadOnlyList<PecaDoLoteDto> AvulsasAbertas, RecusaDto? UltimaRecusa,
    IReadOnlyList<ObrigatoriaDto> Obrigatorias);
