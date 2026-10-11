namespace ViaPermanente.Dominio.Models.Campo;

/// <summary>INSPECAO: checklist em colunas fixas, com versão. Data, início, fim, duração e foto nunca mudam.</summary>
public class Inspecao : IRegistroDeCampo
{
    public int IdInspecao { get; set; }
    public int IdDormente { get; set; }
    public int IdUsuario { get; set; }
    /// <summary>Nulo: rotina.</summary>
    public int? IdAlerta { get; set; }
    /// <summary>Retrabalho: a inspeção que esta refaz.</summary>
    public int? IdInspecaoOrigem { get; set; }
    public DateTime IniciadoEm { get; set; }
    public DateTime FinalizadoEm { get; set; }
    public int DuracaoS { get; set; }
    public int VersaoChecklist { get; set; }
    public EstadoConservacao EstadoConservacao { get; set; }
    public RachaduraInspecao Rachadura { get; set; }
    public AtaqueBiologicoInspecao AtaqueBiologico { get; set; }
    public decimal? UmidadePct { get; set; }
    public int? VidaUtilMeses { get; set; }
    public bool TipoMadeiraConfere { get; set; }
    public ResultadoInspecao Resultado { get; set; }
    public string? Observacao { get; set; }
    public string FotoArquivo { get; set; } = "";
    public Guid? IdCliente { get; set; }
    public DateTime RegistradoEm { get; set; }
    public DateTime? RecebidoEm { get; set; }
}