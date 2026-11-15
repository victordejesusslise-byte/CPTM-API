
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>Como a espécie foi definida (CLASSIFICACAO_MADEIRA.metodo).</summary>
public enum MetodoClassificacao
{
    /// <summary>A espécie da plaquinha do feixe; é a primeira, gravada no cadastro.</summary>
    PlacaFornecedor,
    /// <summary>O especialista comparou com uma amostra conhecida.</summary>
    Comparacao,
    /// <summary>O especialista olhou a madeira.</summary>
    Visual,
    /// <summary>Conferida no catálogo de espécies.</summary>
    Catalogo,
    /// <summary>Análise de laboratório.</summary>
    Laboratorio,
    /// <summary>A pistola NIR (infravermelho próximo): espécie, umidade e propriedades da madeira. Desde 2026-10-13.</summary>
    Nir
}
