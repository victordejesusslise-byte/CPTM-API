
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>Como a espécie foi definida.</summary>
public enum MetodoClassificacao
{
    PlacaFornecedor,
    Comparacao,
    Visual,
    Catalogo,
    Laboratorio,
    /// <summary>A pistola NIR (infravermelho próximo): espécie, umidade e propriedades da madeira. Desde 2026-10-13.</summary>
    Nir
}
