namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>Um alerta da fila (aba Imagem ou Sensores), com a peça, se já se sabe qual é.</summary>
/// <param name="Id">id do alerta (→ ALERTA).</param>
/// <param name="Origem">Sensor (o carro mediu) ou Imagem (a câmera viu).</param>
/// <param name="Tipo">o defeito: Rachadura, Posicao, TagMuda, Fenda ou Quebra.</param>
/// <param name="Status">Aberto, Confirmado ou Descartado.</param>
/// <param name="Data">quando o carro viu o problema.</param>
/// <param name="DesvioMm">o desvio de posição medido, em mm; nulo no alerta de imagem.</param>
/// <param name="Km">o km onde o carro viu, em km com 3 casas.</param>
/// <param name="IdDormente">id da peça (→ DORMENTE); nulo enquanto não se sabe qual é.</param>
/// <param name="CodigoPeca">código impresso da peça: DM-E(empresa)-L(lote)-(id da peça), ex.: DM-E01-L01-0028.</param>
/// <param name="Tag">a tag UHF ativa da peça.</param>
/// <param name="Deteccao">o quadro da câmera com a peça marcada; só no alerta de imagem.</param>
/// <param name="Vistorias">quantas vezes alguém foi ao local (→ VISTORIA_ALERTA).</param>
/// <param name="IdUsuarioDecisao">quem decidiu o alerta (→ USUARIO).</param>
/// <param name="DataDecisao">quando o alerta foi decidido.</param>
/// <param name="MotivoDescarte">por que o alerta foi descartado.</param>
/// <param name="NotaPeca">a nota da peça: Bom, Regular, Ruim ou Crítico; nula sem avaliação.</param>
public record AlertaDto(int Id, string Origem, string Tipo, string Status, DateTime Data, decimal? DesvioMm, decimal? Km,
    int? IdDormente, string? CodigoPeca, string? Tag, DeteccaoDto? Deteccao, int Vistorias,
    int? IdUsuarioDecisao, DateTime? DataDecisao, string? MotivoDescarte, string? NotaPeca = null);