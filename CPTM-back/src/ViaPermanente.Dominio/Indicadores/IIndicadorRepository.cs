using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Indicadores;

/// <summary>Os dados brutos dos indicadores, já filtrados pelo período; as contas ficam no IndicadorService.</summary>
public interface IIndicadorRepository
{
    /// <summary>Alertas decididos no período, com o defeito e a versão do modelo quando vieram da câmera.</summary>
    Task<IReadOnlyList<(OrigemAlerta Origem, StatusAlerta Status, TipoAlerta Tipo, string? VersaoModelo)>> AlertasDecididos(DateTime de, DateTime ate);
    Task<IReadOnlyList<(string Campo, bool Corrigido, int Versao)>> CamposOcrConfirmados(DateTime de, DateTime ate);
    Task<IReadOnlyList<(int? IdModo, bool Ambigua)>> LeiturasDoModulo(DateTime de, DateTime ate);
    Task<IReadOnlyList<Troca>> Trocas();
    /// <summary>Chegadas do período: o informado, o que a nota diz (nulo sem nota) e a soma dos feixes.</summary>
    Task<IReadOnlyList<(int IdRecebimento, int Informada, int? Nota, int Feixes)>> Chegadas(DateTime de, DateTime ate);
    Task<IReadOnlyList<ConferenciaArmazem>> UltimasConferenciasConcluidas();
    /// <summary>Peças cuja tag não respondeu ao carro desde a data, com o número de noites (passagens) em que falhou.</summary>
    Task<IReadOnlyList<(int IdDormente, int Noites)>> FalhasDeTag(DateTime desde);
    Task<IReadOnlyList<ResultadoGravacao>> Gravacoes(DateTime de, DateTime ate);
    Task<IReadOnlyList<Inspecao>> Inspecoes(DateTime de, DateTime ate);
    /// <summary>Revisões por laboratório ou catálogo de uma classificação visual: se confirmaram a espécie anterior.</summary>
    Task<IReadOnlyList<bool>> RevisoesDaClassificacaoVisual(DateTime de, DateTime ate);
    Task<IReadOnlyList<(int IdEmpresa, bool Reprovada)>> VerificacoesPorFornecedor(DateTime de, DateTime ate);
}