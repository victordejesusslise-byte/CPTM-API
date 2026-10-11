using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Dtos.Pecas;
using ViaPermanente.Dominio.Models.Campo;

namespace ViaPermanente.Dominio.Dtos.Inspecoes;

/// <summary>O checklist da inspeção (versão 2), com a peça lida e a foto. O resultado sai das respostas.</summary>
public class NovaInspecaoDto
{
    [Range(1, int.MaxValue)] public int IdDormente { get; init; }
    /// <summary>"Confere" no alerta abre a inspeção ligada a ele. Nulo: inspeção de rotina.</summary>
    public int? IdAlerta { get; init; }
    /// <summary>Retrabalho: a inspeção que esta refaz.</summary>
    public int? IdInspecaoOrigem { get; init; }
    [Required] public LeituraDto Leitura { get; init; } = new();
    public DateTime IniciadoEm { get; init; }
    public DateTime FinalizadoEm { get; init; }
    public EstadoConservacao EstadoConservacao { get; init; }
    public RachaduraInspecao Rachadura { get; init; }
    public AtaqueBiologicoInspecao AtaqueBiologico { get; init; }
    public decimal? UmidadePct { get; init; }
    public int? VidaUtilMeses { get; init; }
    public bool TipoMadeiraConfere { get; init; } = true;
    public string? Observacao { get; init; }
    public string FotoArquivo { get; init; } = "";
    public Guid? IdCliente { get; init; }
    public DateTime? RegistradoEm { get; init; }
}