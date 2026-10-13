using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Via.Dtos;

namespace ViaPermanente.Dominio.Via;

/// <summary>
/// A tela do trecho (pedido do Victor em 2026-10-12): as peças assentadas, cada uma com o ID de posição e a nota única,
/// juntas em faixas de km (100 m por padrão) com a contagem de cada nota.
/// </summary>
public class TrechoMapaService(ITrechoMapaRepository repo, PecaService pecas)
{
    public const int FaixaPadraoM = 100, FaixaMinimaM = 50, FaixaMaximaM = 5000, MaximoDeFaixas = 400;

    public async Task<IReadOnlyList<TrechoDoMapaDto>> Trechos()
    {
        var naVia = await repo.PecasNaVia();
        var porId = await pecas.PorIds(naVia.Select(p => p.IdDormente).ToList());
        return (await repo.Trechos()).Select(t =>
        {
            var doTrecho = naVia.Where(p => p.IdTrecho == t.IdTrecho).Select(p => porId.GetValueOrDefault(p.IdDormente)).OfType<PecaDto>().ToList();
            return Resumo(t, doTrecho);
        }).ToList();
    }

    public async Task<MapaDoTrechoDto?> Mapa(int idTrecho, int faixaM = FaixaPadraoM)
    {
        var trecho = (await repo.Trechos()).FirstOrDefault(t => t.IdTrecho == idTrecho);
        if (trecho is null) return null;
        var ids = (await repo.PecasNaVia(idTrecho)).Select(p => p.IdDormente).ToList();
        var doTrecho = (await pecas.PorIds(ids)).Values.ToList();
        var faixa = Math.Clamp(faixaM, FaixaMinimaM, FaixaMaximaM);
        return new MapaDoTrechoDto(Resumo(trecho, doTrecho), faixa,
            Faixas(trecho.KmInicio, trecho.KmFim, faixa, doTrecho.Select(p => new PecaNoTrechoDto(p.Id, p.Codigo, p.Tag, p.Posicao, p.KmProjeto!.Value, p.Nota)).ToList()));
    }

    /// <summary>Corta o trecho em faixas de <paramref name="faixaM"/> metros; a peça fora dos km das estações estica o começo ou o fim.</summary>
    public static IReadOnlyList<FaixaDoTrechoDto> Faixas(decimal kmInicio, decimal kmFim, int faixaM, IReadOnlyList<PecaNoTrechoDto> pecasNoTrecho)
    {
        var passo = faixaM / 1000m;
        var menor = Math.Min(Math.Min(kmInicio, kmFim), pecasNoTrecho.Count == 0 ? decimal.MaxValue : pecasNoTrecho.Min(p => p.Km));
        var maior = Math.Max(Math.Max(kmInicio, kmFim), pecasNoTrecho.Count == 0 ? decimal.MinValue : pecasNoTrecho.Max(p => p.Km));
        var comeco = Math.Floor(menor / passo) * passo;
        var quantas = Math.Clamp((int)Math.Ceiling((maior - comeco) / passo), 1, MaximoDeFaixas);
        var grupos = pecasNoTrecho
            .GroupBy(p => Math.Clamp((int)Math.Floor((p.Km - comeco) / passo), 0, quantas - 1))
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Km).ToList());
        return Enumerable.Range(0, quantas).Select(i =>
        {
            var lista = grupos.GetValueOrDefault(i) ?? [];
            int Com(string? nota) => lista.Count(p => p.Nota == nota);
            return new FaixaDoTrechoDto(comeco + i * passo, comeco + (i + 1) * passo,
                Com(NotaDaPeca.Bom), Com(NotaDaPeca.Regular), Com(NotaDaPeca.Ruim), Com(NotaDaPeca.Critico), Com(null), lista);
        }).ToList();
    }

    private static TrechoDoMapaDto Resumo(TrechoDaVia t, IReadOnlyList<PecaDto> doTrecho)
        => new(t.IdTrecho, $"{t.Inicio} → {t.Fim}", t.Linha, t.KmInicio, t.KmFim, doTrecho.Count,
            doTrecho.Count(p => p.Nota is NotaDaPeca.Ruim or NotaDaPeca.Critico));
}
