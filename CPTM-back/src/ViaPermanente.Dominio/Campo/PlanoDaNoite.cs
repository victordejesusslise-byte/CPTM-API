using System.Globalization;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// O plano da noite (Victor, 2026-10-24): o supervisor aprova as trocas até o corte (parâmetro troca_plano_horario, 20:00) e elas entram
/// na troca daquela noite; aprovada depois do corte, vai para a noite seguinte, a não ser que seja urgente. Do corte até o fim da noite
/// (06:00 do dia seguinte), o plano trava: nada que está nele é cancelado.
/// </summary>
public static class PlanoDaNoite
{
    public const string ChaveHorario = "troca_plano_horario";
    public static readonly TimeOnly CortePadrao = new(20, 0);
    /// <summary>Quando a noite do plano acaba (a manhã do dia seguinte).</summary>
    public static readonly TimeOnly FimDaNoite = new(6, 0);

    /// <summary>O corte do parâmetro ("20:00"); valor inválido vale 20:00.</summary>
    public static TimeOnly Corte(string? valor)
        => TimeOnly.TryParseExact(valor?.Trim(), ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var corte) ? corte : CortePadrao;

    /// <summary>A noite em que a troca aprovada agora entra: a do dia, até o corte ou se urgente; senão, a do dia seguinte.</summary>
    public static DateOnly NoiteDe(DateTime aprovadaEm, TimeOnly corte, bool urgente)
    {
        var dia = DateOnly.FromDateTime(aprovadaEm);
        return urgente || TimeOnly.FromDateTime(aprovadaEm) <= corte ? dia : dia.AddDays(1);
    }

    /// <summary>A noite que está valendo agora: antes do fim da noite (06:00) ainda é a noite de ontem.</summary>
    public static DateOnly NoiteAtual(DateTime agora)
    {
        var dia = DateOnly.FromDateTime(agora);
        return TimeOnly.FromDateTime(agora) < FimDaNoite ? dia.AddDays(-1) : dia;
    }

    /// <summary>A troca está no plano de uma noite que já fechou e ainda não acabou: não pode ser cancelada agora.</summary>
    public static bool Travada(DateOnly? noite, DateTime agora, TimeOnly corte)
        => noite is DateOnly n && agora >= n.ToDateTime(corte) && agora < n.AddDays(1).ToDateTime(FimDaNoite);
}
