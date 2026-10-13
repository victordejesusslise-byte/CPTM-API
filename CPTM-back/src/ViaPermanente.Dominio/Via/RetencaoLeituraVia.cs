
namespace ViaPermanente.Dominio.Via;

/// <summary>
/// 90 dias de passagem: LEITURA_VIA vive em partições de uma semana (começam na segunda). A manutenção mantém
/// semanas abertas à frente e descarta, semana inteira, o que já passou de 90 dias (fica entre 90 e 97 dias).
/// </summary>
public static class RetencaoLeituraVia
{
    public const int DiasDeRetencao = 90;
    public const int SemanasAFrente = 4;

    public static DateTime Segunda(DateTime dia) => dia.Date.AddDays(-(((int)dia.DayOfWeek + 6) % 7));

    /// <summary>As fronteiras (segundas) que faltam para cobrir até SemanasAFrente semanas depois de hoje.</summary>
    public static IReadOnlyList<DateTime> FronteirasNovas(IReadOnlyList<DateTime> atuais, DateTime hoje)
    {
        var ultima = atuais.Count > 0 ? atuais.Max() : Segunda(hoje);
        var ate = Segunda(hoje).AddDays(7 * SemanasAFrente);
        var novas = new List<DateTime>();
        for (var d = ultima.AddDays(7); d <= ate; d = d.AddDays(7))
            novas.Add(d);
        return novas;
    }

    /// <summary>
    /// As fronteiras a descartar, da mais velha para a mais nova: enquanto a primeira fronteira for anterior ao limite de 90 dias,
    /// a primeira partição (tudo antes dela) é esvaziada e a fronteira some. Fica sempre pelo menos uma fronteira.
    /// </summary>
    public static IReadOnlyList<DateTime> FronteirasParaDescartar(IReadOnlyList<DateTime> atuais, DateTime hoje)
    {
        var limite = hoje.Date.AddDays(-DiasDeRetencao);
        return atuais.OrderBy(d => d).Where(d => d <= limite).Take(Math.Max(0, atuais.Count - 1)).ToList();
    }
}