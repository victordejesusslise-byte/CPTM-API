using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Dominio.Pecas;

/// <summary>O que se sabe da peça para dar a nota e a posição: a última verificação, a última inspeção, o último alerta e o lugar na via.</summary>
public record SinaisDaPeca(
    DateTime? QualidadeData, ResultadoVerificacao? QualidadeResultado, RachaduraVerificacao? QualidadeRachadura, AtaqueBiologicoVerificacao? QualidadeAtaque,
    DateTime? InspecaoData, EstadoConservacao? InspecaoEstado, ResultadoInspecao? InspecaoResultado,
    DateTime? AlertaData, TipoAlerta? AlertaTipo, StatusAlerta? AlertaStatus,
    string? Linha, int? NumeroNoTrecho);

/// <summary>
/// A nota única de estado da peça (pedido do Victor em 2026-10-12): Bom, Regular, Ruim ou Crítico, a mesma na qualidade,
/// na inspeção, na passagem, no estoque e no painel. Vale a avaliação mais recente entre a verificação de qualidade,
/// a inspeção e o alerta aberto ou confirmado (o descartado não conta).
/// </summary>
public static class NotaDaPeca
{
    public const string Bom = "Bom", Regular = "Regular", Ruim = "Ruim", Critico = "Crítico";

    public static string? Calcular(SinaisDaPeca? s)
    {
        if (s is null) return null;
        var avaliacoes = new List<(DateTime Data, string Nota)>();
        if (s.QualidadeData is DateTime dq && s.QualidadeResultado is { } rq)
            avaliacoes.Add((dq, rq == ResultadoVerificacao.Reprovada ? Critico
                : s.QualidadeRachadura is RachaduraVerificacao.Leve or RachaduraVerificacao.Grave || s.QualidadeAtaque is AtaqueBiologicoVerificacao.Suspeito or AtaqueBiologicoVerificacao.Presente ? Regular
                : Bom));
        if (s.InspecaoData is DateTime di && s.InspecaoEstado is { } estado)
            avaliacoes.Add((di, s.InspecaoResultado == ResultadoInspecao.Reprovada ? Critico
                : estado switch { EstadoConservacao.Bom => Bom, EstadoConservacao.Regular => Regular, _ => Ruim }));
        if (s.AlertaData is DateTime da && s.AlertaTipo is { } tipo && s.AlertaStatus is StatusAlerta.Aberto or StatusAlerta.Confirmado)
            avaliacoes.Add((da, tipo is TipoAlerta.Rachadura or TipoAlerta.Fenda or TipoAlerta.Quebra && s.AlertaStatus == StatusAlerta.Confirmado ? Critico : Ruim));
        return avaliacoes.Count == 0 ? null : avaliacoes.MaxBy(a => a.Data).Nota;
    }

    /// <summary>O ID de posição na via: L09-213100-0012 = linha 9 · km 213+100 · 12ª peça do trecho. Nulo fora da via.</summary>
    public static string? Posicao(string? linha, decimal? km, int? numeroNoTrecho)
    {
        if (km is null || numeroNoTrecho is null) return null;
        var digitos = new string((linha ?? "").SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
        var numeroLinha = int.TryParse(digitos, out var n) ? n : 0;
        return $"L{numeroLinha:00}-{(int)Math.Round(km.Value * 1000):000000}-{numeroNoTrecho:0000}";
    }
}
