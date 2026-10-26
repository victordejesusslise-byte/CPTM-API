namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// Uma peça na cópia das linhas do celular (app, Entrega 2): só o essencial para consultar e inspecionar sem rede.
/// A posição (L09-213100-0012) já diz a linha, o km e o número da peça no trecho.
/// </summary>
public record PecaDaLinhaDto(int Id, string Codigo, string? Tag, decimal? Km, string? Posicao, int? IdTrecho, string Situacao, string? Nota,
    string? UltimaInspecao, DateTime? UltimaInspecaoEm);

/// <summary>
/// Um pedaço da cópia. Próximo: o último id da página (pedir de novo com depoisDe = próximo); nulo quando acabou.
/// GeradoEm: a hora em que a busca começou; o celular guarda e, da próxima vez, pede "desde" ela.
/// </summary>
public record PaginaDaLinhaDto(IReadOnlyList<PecaDaLinhaDto> Pecas, int? Proximo, DateTime GeradoEm);
