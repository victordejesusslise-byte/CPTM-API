using ViaPermanente.Dominio.Ambiental.Models;

namespace ViaPermanente.Dominio.Integracoes;

/// <summary>
/// A planilha de notas fiscais que a API deles gera. Modelo PROVISÓRIO (escolha do Victor, 2026-10-11), uma linha por item:
/// Pedido | Nota | Emissão | Valor | Espécie | Comprimento (m) | Classe | Quantidade | Volume (m³). Quando o modelo real chegar, muda só aqui.
/// </summary>
public interface IPlanilhaNotaRepository
{
    IReadOnlyList<LinhaNotaPlanilha> Ler(Stream planilha);
    /// <summary>A planilha em branco, só com os cabeçalhos.</summary>
    byte[] Modelo();
}