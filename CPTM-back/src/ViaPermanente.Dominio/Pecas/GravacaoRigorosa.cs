using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas;

/// <summary>
/// Leitura tolerante, gravação rigorosa: nunca gravar na primeira tag encontrada. Com mais de uma tag
/// no alcance a gravação é bloqueada; ela só vale quando o número relido é igual ao gravado.
/// O servidor confere de novo o que o celular mandou.
/// </summary>
public static class GravacaoRigorosa
{
    public static ResultadoGravacao Resultado(int tagsVistas, string tagGravada, string? tagRelida)
        => tagsVistas != 1 ? ResultadoGravacao.Bloqueada
            : string.IsNullOrWhiteSpace(tagRelida) ? ResultadoGravacao.Falhou
            : tagRelida.Trim() != tagGravada ? ResultadoGravacao.Divergente
            : ResultadoGravacao.Ok;

    public static string Mensagem(ResultadoGravacao resultado, string tagGravada, string? tagRelida) => resultado switch
    {
        ResultadoGravacao.Bloqueada => "Mais de uma tag no alcance: nada foi gravado. Aproxime o módulo de uma peça só.",
        ResultadoGravacao.Falhou => "A releitura não trouxe número: a tag não foi vinculada. Tente de novo com o mesmo número.",
        ResultadoGravacao.Divergente => $"A releitura trouxe {tagRelida?.Trim()}, diferente de {tagGravada}: a tag não foi vinculada, e o número {tagGravada} não volta a ser usado.",
        _ => $"Peça cadastrada com a tag {tagGravada}."
    };
}