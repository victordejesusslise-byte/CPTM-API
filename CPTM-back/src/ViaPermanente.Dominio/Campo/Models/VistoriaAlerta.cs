using ViaPermanente.Dominio.Comum;
namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>VISTORIA_ALERTA: uma linha por ida ao local; não fecha o alerta.</summary>
public class VistoriaAlerta : IRegistroDeCampo
{
    /// <summary>id da vistoria (chave de VISTORIA_ALERTA).</summary>
    public int IdVistoria { get; set; }
    /// <summary>o alerta vistoriado (→ ALERTA).</summary>
    public int IdAlerta { get; set; }
    /// <summary>quem foi ao local (→ USUARIO): operador da via ou técnico.</summary>
    public int IdUsuario { get; set; }
    /// <summary>quando a vistoria foi registrada no servidor.</summary>
    public DateTime Data { get; set; }
    /// <summary>Sim: confirmo o defeito. Não: não vi defeito.</summary>
    public bool Viu { get; set; }
    /// <summary>Quando o alerta não tinha peça.</summary>
    public string? TagLida { get; set; }
    /// <summary>o caminho da foto do local (obrigatória).</summary>
    public string FotoArquivo { get; set; } = "";
    /// <summary>o que a pessoa anotou.</summary>
    public string? Observacao { get; set; }
    /// <summary>gerado no celular: o mesmo envio entra uma vez só.</summary>
    public Guid? IdCliente { get; set; }
    /// <summary>a hora do celular, quando a vistoria foi feita.</summary>
    public DateTime RegistradoEm { get; set; }
    /// <summary>a hora em que chegou ao servidor.</summary>
    public DateTime? RecebidoEm { get; set; }
}