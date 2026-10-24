namespace ViaPermanente.Dominio.Catalogos.Models;

/// <summary>
/// Os comprimentos de dormente, fixos no código (Victor, 2026-10-23: saiu a tabela TAMANHO_DORMENTE). O número é o que vai no banco
/// (id_tamanho do feixe, do pedido, da nota, da oferta e da pilha) e na API. Comprimento novo: acrescentar em <see cref="Todos"/> e gerar a
/// migração, que atualiza a regra do banco. A largura e a altura são do fornecedor (MEDIDA_FORNECEDOR), não do comprimento.
/// </summary>
public class TamanhoDormente
{
    public int IdTamanho { get; init; }
    public decimal ComprimentoM { get; init; }

    public static readonly IReadOnlyList<TamanhoDormente> Todos =
    [
        new() { IdTamanho = 1, ComprimentoM = 2.80m },
        new() { IdTamanho = 2, ComprimentoM = 3.00m },
        new() { IdTamanho = 3, ComprimentoM = 4.00m },
        new() { IdTamanho = 4, ComprimentoM = 4.80m },
    ];

    /// <summary>O comprimento do número (0 se o número não está na lista).</summary>
    public static decimal Comprimento(int idTamanho) => Todos.FirstOrDefault(t => t.IdTamanho == idTamanho)?.ComprimentoM ?? 0m;
}
