using ViaPermanente.Dominio.Campo.Models;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// O checklist da inspeção, em colunas fixas e com versão (Checklist em colunas, com versão). Versão 2:
/// reprova com conservação ruim, rachadura grave, ataque biológico avançado ou vida útil de 6 meses ou menos.
/// </summary>
public static class ChecklistInspecao
{
    /// <summary>a versão das perguntas que vai em cada inspeção nova.</summary>
    public const int VersaoAtual = 2;
    /// <summary>com esta vida útil (em meses) ou menos, a peça reprova.</summary>
    public const int VidaUtilMinimaMeses = 6;

    /// <summary>A lista do que reprova a peça; vazia quando a peça passa.</summary>
    public static IReadOnlyList<string> MotivosDeReprovacao(EstadoConservacao conservacao, RachaduraInspecao rachadura,
        AtaqueBiologicoInspecao ataque, int? vidaUtilMeses)
    {
        // Cada resposta ruim entra como um motivo.
        var motivos = new List<string>();
        if (conservacao == EstadoConservacao.Ruim) motivos.Add("estado de conservação ruim");
        if (rachadura == RachaduraInspecao.Grave) motivos.Add("rachadura grave");
        if (ataque == AtaqueBiologicoInspecao.Avancado) motivos.Add("ataque biológico avançado");
        // Sem vida útil informada, essa regra não vale.
        if (vidaUtilMeses <= VidaUtilMinimaMeses) motivos.Add($"vida útil de {vidaUtilMeses} meses");
        return motivos;
    }

    /// <summary>Reprovada se há algum motivo; senão, aprovada.</summary>
    public static ResultadoInspecao Resultado(EstadoConservacao conservacao, RachaduraInspecao rachadura,
        AtaqueBiologicoInspecao ataque, int? vidaUtilMeses)
        => MotivosDeReprovacao(conservacao, rachadura, ataque, vidaUtilMeses).Count > 0 ? ResultadoInspecao.Reprovada : ResultadoInspecao.Aprovada;
}