using ViaPermanente.Dominio.Comum;
namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>INSPECAO: checklist em colunas fixas, com versão. Data, início, fim, duração e foto nunca mudam.</summary>
public class Inspecao : IRegistroDeCampo
{
    /// <summary>id da inspeção (chave de INSPECAO).</summary>
    public int IdInspecao { get; set; }
    /// <summary>id da peça inspecionada (→ DORMENTE).</summary>
    public int IdDormente { get; set; }
    /// <summary>o técnico que fez a inspeção (→ USUARIO).</summary>
    public int IdUsuario { get; set; }
    /// <summary>Nulo: rotina.</summary>
    public int? IdAlerta { get; set; }
    /// <summary>Retrabalho: a inspeção que esta refaz.</summary>
    public int? IdInspecaoOrigem { get; set; }
    /// <summary>quando o checklist começou.</summary>
    public DateTime IniciadoEm { get; set; }
    /// <summary>quando o checklist terminou.</summary>
    public DateTime FinalizadoEm { get; set; }
    /// <summary>a duração do checklist, em segundos.</summary>
    public int DuracaoS { get; set; }
    /// <summary>a versão das perguntas do checklist.</summary>
    public int VersaoChecklist { get; set; }
    /// <summary>Bom, Regular ou Ruim.</summary>
    public EstadoConservacao EstadoConservacao { get; set; }
    /// <summary>Nenhuma, Leve, Media ou Grave.</summary>
    public RachaduraInspecao Rachadura { get; set; }
    /// <summary>Nenhum, Inicio ou Avancado.</summary>
    public AtaqueBiologicoInspecao AtaqueBiologico { get; set; }
    /// <summary>a umidade da madeira, em %.</summary>
    public decimal? UmidadePct { get; set; }
    /// <summary>a vida útil que ainda resta, em meses.</summary>
    public int? VidaUtilMeses { get; set; }
    /// <summary>se a madeira confere com a espécie cadastrada.</summary>
    public bool TipoMadeiraConfere { get; set; }
    /// <summary>Aprovada ou Reprovada.</summary>
    public ResultadoInspecao Resultado { get; set; }
    /// <summary>o que o técnico anotou.</summary>
    public string? Observacao { get; set; }
    /// <summary>o caminho da foto da peça (obrigatória).</summary>
    public string FotoArquivo { get; set; } = "";
    /// <summary>gerado no celular: o mesmo envio entra uma vez só.</summary>
    public Guid? IdCliente { get; set; }
    /// <summary>a hora do celular, quando a inspeção foi feita.</summary>
    public DateTime RegistradoEm { get; set; }
    /// <summary>a hora em que chegou ao servidor.</summary>
    public DateTime? RecebidoEm { get; set; }
}