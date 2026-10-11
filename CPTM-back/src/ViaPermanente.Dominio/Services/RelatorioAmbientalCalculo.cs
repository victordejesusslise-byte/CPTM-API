using ViaPermanente.Dominio.Models.Ambiental;

namespace ViaPermanente.Dominio.Services;

/// <summary>
/// O estoque mensal por espécie para o IBAMA: m³ em estoque no fim do mês, m³ que entrou e que saiu no mês, e a idade média
/// (dias desde a entrada) das peças em estoque. Só entram peças aprovadas que passaram pelo armazém: reprovadas e retiradas ficam fora.
/// </summary>
public static class RelatorioAmbientalCalculo
{
    public static IReadOnlyList<RelatorioAmbientalItem> Itens(DateOnly mes, IReadOnlyList<PecaParaRelatorio> pecas)
    {
        var inicio = new DateTime(mes.Year, mes.Month, 1);
        var fim = inicio.AddMonths(1);
        bool EmEstoque(PecaParaRelatorio p) => p.EntradaEm < fim && (p.SaidaEm is null || p.SaidaEm >= fim);

        return pecas.Where(p => p.EntradaEm is not null)
            .GroupBy(p => p.IdEspecie)
            .Select(g =>
            {
                var estoque = g.Where(EmEstoque).ToList();
                return new RelatorioAmbientalItem
                {
                    IdEspecie = g.Key,
                    VolumeM3Estoque = estoque.Sum(p => p.M3),
                    VolumeM3Entrada = g.Where(p => p.EntradaEm >= inicio && p.EntradaEm < fim).Sum(p => p.M3),
                    VolumeM3Saida = g.Where(p => p.SaidaEm >= inicio && p.SaidaEm < fim).Sum(p => p.M3),
                    IdadeMediaDias = estoque.Count == 0 ? 0 : (int)Math.Round(estoque.Average(p => (fim - p.EntradaEm!.Value).TotalDays))
                };
            })
            .Where(i => i.VolumeM3Estoque > 0 || i.VolumeM3Entrada > 0 || i.VolumeM3Saida > 0)
            .OrderBy(i => i.IdEspecie)
            .ToList();
    }
}