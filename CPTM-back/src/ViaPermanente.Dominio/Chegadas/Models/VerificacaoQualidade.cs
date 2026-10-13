using ViaPermanente.Dominio.Comum;
namespace ViaPermanente.Dominio.Chegadas.Models;

/// <summary>VERIFICACAO_QUALIDADE: peça a peça, depois da UHF; só insere.</summary>
public class VerificacaoQualidade : IRegistroDeCampo
{
    public int IdVerificacao { get; set; }
    public int IdDormente { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Data { get; set; }
    public AtaqueBiologicoVerificacao AtaqueBiologico { get; set; }
    public RachaduraVerificacao Rachadura { get; set; }
    public bool PlacaAntirrachante { get; set; }
    public bool PlacaConfere { get; set; }
    public ResultadoVerificacao Resultado { get; set; }
    /// <summary>Obrigatório se reprovada.</summary>
    public string? Motivo { get; set; }
    /// <summary>Obrigatória se reprovada.</summary>
    public string? FotoArquivo { get; set; }
    public DestinoVerificacao Destino { get; set; }
    public Guid? IdCliente { get; set; }
    public DateTime RegistradoEm { get; set; }
    public DateTime? RecebidoEm { get; set; }
}