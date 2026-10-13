using ViaPermanente.Dominio.Campo.Models;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// Corrigir não é refazer: só quem fez, com motivo, muda as respostas do checklist. Data, início, fim, duração e foto não mudam.
/// Se a correção aprova a peça, a troca aberta pela inspeção é cancelada.
/// </summary>
public class CorrigirInspecaoDto
{
    public EstadoConservacao EstadoConservacao { get; init; }
    public RachaduraInspecao Rachadura { get; init; }
    public AtaqueBiologicoInspecao AtaqueBiologico { get; init; }
    public decimal? UmidadePct { get; init; }
    public int? VidaUtilMeses { get; init; }
    public bool TipoMadeiraConfere { get; init; } = true;
    public string? Observacao { get; init; }
    public string Motivo { get; init; } = "";
}