using ViaPermanente.Dominio.Campo.Models;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// Corrigir não é refazer: só quem fez, com motivo, muda as respostas do checklist. Data, início, fim, duração e foto não mudam.
/// Se a correção aprova a peça, a troca aberta pela inspeção é cancelada.
/// </summary>
public class CorrigirInspecaoDto
{
    /// <summary>a resposta corrigida do estado de conservação: Bom, Regular ou Ruim (Ruim reprova).</summary>
    public EstadoConservacao EstadoConservacao { get; init; }
    /// <summary>a rachadura corrigida: Nenhuma, Leve, Media ou Grave (Grave reprova).</summary>
    public RachaduraInspecao Rachadura { get; init; }
    /// <summary>o ataque biológico corrigido: Nenhum, Inicio ou Avancado (Avancado reprova).</summary>
    public AtaqueBiologicoInspecao AtaqueBiologico { get; init; }
    /// <summary>a umidade da madeira, em %; nula se não foi medida.</summary>
    public decimal? UmidadePct { get; init; }
    /// <summary>a vida útil que ainda resta, em meses (6 ou menos reprova); nula se não foi estimada.</summary>
    public int? VidaUtilMeses { get; init; }
    /// <summary>se a madeira vista confere com a espécie cadastrada da peça.</summary>
    public bool TipoMadeiraConfere { get; init; } = true;
    /// <summary>a observação nova; em branco, fica a que já estava.</summary>
    public string? Observacao { get; init; }
    /// <summary>por que a inspeção está sendo corrigida; obrigatório.</summary>
    public string Motivo { get; init; } = "";
}