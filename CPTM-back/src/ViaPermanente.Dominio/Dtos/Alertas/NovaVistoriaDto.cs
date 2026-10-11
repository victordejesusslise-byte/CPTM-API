using ViaPermanente.Dominio.Dtos.Pecas;

namespace ViaPermanente.Dominio.Dtos.Alertas;

/// <summary>
/// A ida ao local, sempre com foto. Lê a tag da peça com o módulo; se a tag não responde, marca e vai pelo km e pela foto.
/// No alerta sem peça, a tag lida dá a peça ao alerta. A vistoria não fecha o alerta.
/// </summary>
public class NovaVistoriaDto
{
    /// <summary>Sim: confirmo o defeito. Não: não vi defeito.</summary>
    public bool Viu { get; init; }
    public LeituraDto? Leitura { get; init; }
    public bool TagNaoResponde { get; init; }
    public string FotoArquivo { get; init; } = "";
    public string? Observacao { get; init; }
    public Guid? IdCliente { get; init; }
    public DateTime? RegistradoEm { get; init; }
}