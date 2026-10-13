using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dominio.Indicadores;

/// <summary>
/// Quem vê cada indicador: cada cargo vê os da sua parte do trabalho; o supervisor vê todos.
/// (Pedido do Victor em 2026-10-12; a divisão é a sugerida pelo Claude, ajustável aqui.)
/// </summary>
public static class IndicadoresPorCargo
{
    private static readonly Dictionary<Cargo, HashSet<string>> DoCargo = new()
    {
        [Cargo.OperadorPatio] = ["divergencia-nota-chegada", "divergencia-conferencia", "estoque-m3", "gravacao-conferida", "segunda-classe-pedido",
            "alvo-unico", "acerto-ocr", "reprovacao-fornecedor"],
        [Cargo.Especialista] = ["reprovacao-fornecedor", "estoque-m3", "identificado-corretamente", "taxa-retrabalho", "acerto-ocr"],
        [Cargo.Tecnico] = ["acerto-camera", "confiabilidade-dados", "dias-peca-condenada", "tempo-passos-troca", "inspecoes-por-dia",
            "tempo-medio-inspecao", "taxa-retrabalho", "falhas-tag-7-noites"],
        [Cargo.OperadorVia] = ["confiabilidade-dados", "falhas-tag-7-noites", "dias-peca-condenada", "tempo-passos-troca"],
    };

    public static bool Ve(Cargo cargo, string chave)
        => cargo == Cargo.Supervisor || (DoCargo.TryGetValue(cargo, out var chaves) && chaves.Contains(chave));
}
