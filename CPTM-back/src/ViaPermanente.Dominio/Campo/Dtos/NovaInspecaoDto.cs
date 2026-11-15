using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>O checklist da inspeção (versão 2), com a peça lida e a foto. O resultado sai das respostas.</summary>
public class NovaInspecaoDto
{
    /// <summary>id da peça inspecionada (→ DORMENTE).</summary>
    [Range(1, int.MaxValue)] public int IdDormente { get; init; }
    /// <summary>"Confere" no alerta abre a inspeção ligada a ele. Nulo: inspeção de rotina.</summary>
    public int? IdAlerta { get; init; }
    /// <summary>Retrabalho: a inspeção que esta refaz.</summary>
    public int? IdInspecaoOrigem { get; init; }
    /// <summary>a leitura da tag da peça com o módulo (alvo único); obrigatória.</summary>
    [Required] public LeituraDto Leitura { get; init; } = new();
    /// <summary>quando o cronômetro do checklist começou; obrigatório.</summary>
    public DateTime IniciadoEm { get; init; }
    /// <summary>quando o checklist terminou; depois do início.</summary>
    public DateTime FinalizadoEm { get; init; }
    /// <summary>Bom, Regular ou Ruim (Ruim reprova).</summary>
    public EstadoConservacao EstadoConservacao { get; init; }
    /// <summary>Nenhuma, Leve, Media ou Grave (Grave reprova).</summary>
    public RachaduraInspecao Rachadura { get; init; }
    /// <summary>Nenhum, Inicio ou Avancado (Avancado reprova).</summary>
    public AtaqueBiologicoInspecao AtaqueBiologico { get; init; }
    /// <summary>a umidade da madeira, em %; nula se não foi medida.</summary>
    public decimal? UmidadePct { get; init; }
    /// <summary>a vida útil que ainda resta, em meses (6 ou menos reprova); nula se não foi estimada.</summary>
    public int? VidaUtilMeses { get; init; }
    /// <summary>se a madeira vista confere com a espécie cadastrada da peça.</summary>
    public bool TipoMadeiraConfere { get; init; } = true;
    /// <summary>o que o técnico anotou.</summary>
    public string? Observacao { get; init; }
    /// <summary>o caminho da foto da peça, devolvido pelo envio da foto; obrigatória (Foto em todo registro de campo).</summary>
    public string FotoArquivo { get; init; } = "";
    /// <summary>gerado no celular: a inspeção enviada de novo devolve a que já entrou, sem duplicar.</summary>
    public Guid? IdCliente { get; init; }
    /// <summary>a hora do campo (feita sem rede, chega depois); nula: a hora do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}