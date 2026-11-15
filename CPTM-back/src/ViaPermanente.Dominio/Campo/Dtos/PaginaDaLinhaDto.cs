namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// Uma peça na cópia das linhas do celular (app, Entrega 2): só o essencial para consultar e inspecionar sem rede.
/// A posição (L09-213100-0012) já diz a linha, o km e o número da peça no trecho.
/// </summary>
/// <param name="Id">id da peça (→ DORMENTE).</param>
/// <param name="Codigo">código impresso da peça: DM-E(empresa)-L(lote)-(id da peça), ex.: DM-E01-L01-0028.</param>
/// <param name="Tag">a tag UHF ativa da peça; nula sem tag ativa.</param>
/// <param name="Km">o km da peça na via, em km com 3 casas.</param>
/// <param name="Posicao">o ID de posição na via: linha, km e nº da peça, ex.: L09-213100-0012.</param>
/// <param name="IdTrecho">o trecho onde a peça está assentada (→ TRECHO).</param>
/// <param name="Situacao">NaVia ou Retirada (o celular apaga a retirada).</param>
/// <param name="Nota">a nota única da peça: Bom, Regular, Ruim ou Crítico; nula sem avaliação.</param>
/// <param name="UltimaInspecao">o resultado da última inspeção (Aprovada ou Reprovada); nulo se nunca foi inspecionada.</param>
/// <param name="UltimaInspecaoEm">quando a última inspeção terminou.</param>
public record PecaDaLinhaDto(int Id, string Codigo, string? Tag, decimal? Km, string? Posicao, int? IdTrecho, string Situacao, string? Nota,
    string? UltimaInspecao, DateTime? UltimaInspecaoEm);

/// <summary>
/// Um pedaço da cópia. Próximo: o último id da página (pedir de novo com depoisDe = próximo); nulo quando acabou.
/// GeradoEm: a hora em que a busca começou; o celular guarda e, da próxima vez, pede "desde" ela.
/// </summary>
/// <param name="Pecas">as peças desta página, na ordem do id.</param>
/// <param name="Proximo">o id da última peça da página, para pedir a próxima; nulo quando acabou.</param>
/// <param name="GeradoEm">a hora em que a busca começou; o celular pede "desde" ela na próxima vez.</param>
public record PaginaDaLinhaDto(IReadOnlyList<PecaDaLinhaDto> Pecas, int? Proximo, DateTime GeradoEm);
