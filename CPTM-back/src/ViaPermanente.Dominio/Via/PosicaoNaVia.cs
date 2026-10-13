using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Via;

/// <summary>
/// O que a leitura da passagem diz da peça. Guarda-se o número medido (Guardar o número medido);
/// os limites vêm de PARAMETRO (desvio_limite_mm, desvio_atencao_mm), provisórios até o Limite de desvio de 40 mm ter dono.
/// </summary>
public static class PosicaoNaVia
{
    public static FaixaPosicao Faixa(decimal desvioMm, decimal atencaoMm, decimal limiteMm)
        => Math.Abs(desvioMm) > limiteMm ? FaixaPosicao.Fora
            : Math.Abs(desvioMm) > atencaoMm ? FaixaPosicao.Atencao
            : FaixaPosicao.Ok;

    /// <summary>O alerta de sensor da leitura, como no protótipo: tag muda, depois suspeita de rachadura, depois posição.</summary>
    public static TipoAlerta? AlertaDaLeitura(bool respondeu, bool rachaduraSuspeita, decimal? desvioMm, decimal limiteMm)
        => !respondeu ? TipoAlerta.TagMuda
            : rachaduraSuspeita ? TipoAlerta.Rachadura
            : desvioMm is decimal d && Math.Abs(d) > limiteMm ? TipoAlerta.Posicao
            : null;

    public static TipoAlerta TipoDoDefeito(DefeitoImagem defeito) => defeito switch
    {
        DefeitoImagem.Fenda => TipoAlerta.Fenda,
        DefeitoImagem.Quebra => TipoAlerta.Quebra,
        _ => TipoAlerta.Rachadura
    };
}