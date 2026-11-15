using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// Quem aprova a troca e o lote de troca: os cargos de PARAMETRO troca_quem_aprova, separados por vírgula (Victor, 2026-10-31: o técnico
/// também aprova, até o pedido que ele mesmo fez).
/// </summary>
public static class QuemAprovaTroca
{
    /// <summary>a chave do parâmetro (→ PARAMETRO), hoje "supervisor,tecnico".</summary>
    public const string Chave = "troca_quem_aprova";

    /// <summary>Recusa a aprovação se o cargo da pessoa não está na lista do parâmetro.</summary>
    public static void Conferir(string? valorDoParametro, Cargo cargo)
    {
        // A lista vem do parâmetro; sem parâmetro, a lista fica vazia e ninguém aprova.
        var quemAprova = (valorDoParametro ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!quemAprova.Contains(cargo.ToString(), StringComparer.OrdinalIgnoreCase))
            throw new RegraDeNegocioException($"Quem aprova a troca: {string.Join(", ", quemAprova)} (parâmetro {Chave}).");
    }
}
