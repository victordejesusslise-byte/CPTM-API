using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// A peça que entra tem o mesmo comprimento da que sai e classe igual ou melhor que a exigida pelo trecho
/// (2ª não entra em trecho de 1ª; 1ª serve em trecho de 2ª). A classe 1 é melhor que a 2.
/// </summary>
public static class CompatibilidadeTroca
{
    public static bool Serve(int idTamanhoDaCandidata, byte classeDaCandidata, int idTamanhoQueSai, byte classeExigida)
        => idTamanhoDaCandidata == idTamanhoQueSai && classeDaCandidata <= classeExigida;

    /// <summary>
    /// As peças do estoque que servem (na pilha, fora das que não podem), do armazém mais perto do km da troca e, nele, a que entrou
    /// primeiro (saída do mais antigo, para a madeira não apodrecer parada). A primeira é a sugerida. Vale para a troca e para o lote.
    /// </summary>
    public static IReadOnlyList<CompativelDto> Ordenar(IEnumerable<PecaDto> estoque, int idTamanho, byte classeExigida, decimal? km,
        IReadOnlyDictionary<int, decimal?> kmDoArmazem, IReadOnlySet<int> fora, DateTime hoje)
        => estoque
            .Where(p => p.IdPilha is not null && !fora.Contains(p.Id) && Serve(p.IdTamanho, p.Classe, idTamanho, classeExigida))
            .Select(p => (Peca: p, Distancia: km is decimal k && kmDoArmazem.GetValueOrDefault(p.IdArmazem) is decimal ka ? Math.Abs(ka - k) : (decimal?)null))
            .OrderBy(x => x.Distancia ?? decimal.MaxValue).ThenBy(x => x.Peca.EntradaArmazemEm ?? DateTime.MaxValue).ThenBy(x => x.Peca.Id)
            .Select((x, i) => new CompativelDto(x.Peca, x.Distancia, x.Peca.EntradaArmazemEm is DateTime e ? (int)(hoje - e).TotalDays : 0, i == 0))
            .ToList();
}
