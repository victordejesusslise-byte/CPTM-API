using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// A ida ao local, sempre com foto. Lê a tag da peça com o módulo; se a tag não responde, marca e vai pelo km e pela foto.
/// No alerta sem peça, a tag lida dá a peça ao alerta. A vistoria não fecha o alerta.
/// </summary>
public class NovaVistoriaDto
{
    /// <summary>Sim: confirmo o defeito. Não: não vi defeito.</summary>
    public bool Viu { get; init; }
    /// <summary>a leitura da tag da peça com o módulo; nula só se TagNaoResponde.</summary>
    public LeituraDto? Leitura { get; init; }
    /// <summary>verdadeiro quando a tag da peça do alerta não responde (só no alerta que já tem peça).</summary>
    public bool TagNaoResponde { get; init; }
    /// <summary>o caminho da foto do local, devolvido pelo envio da foto; obrigatória.</summary>
    public string FotoArquivo { get; init; } = "";
    /// <summary>o que a pessoa anotou no local.</summary>
    public string? Observacao { get; init; }
    /// <summary>gerado no celular: a vistoria enviada de novo não duplica.</summary>
    public Guid? IdCliente { get; init; }
    /// <summary>a hora do campo (feita sem rede, chega depois); nula: a hora do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}