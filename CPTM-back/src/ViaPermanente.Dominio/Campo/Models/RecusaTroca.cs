using ViaPermanente.Dominio.Comum;
namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>RECUSA_TROCA: uma linha por volta; o status da troca (ou do lote) acompanha a última recusa. É de uma troca ou de um lote, nunca dos dois.</summary>
public class RecusaTroca : IRegistroDeCampo
{
    /// <summary>id da recusa (chave de RECUSA_TROCA).</summary>
    public int IdRecusa { get; set; }
    /// <summary>a troca recusada (→ TROCA); nula na recusa do lote.</summary>
    public int? IdTroca { get; set; }
    /// <summary>o lote recusado (→ LOTE_TROCA); nulo na recusa da troca.</summary>
    public int? IdLoteTroca { get; set; }
    /// <summary>o operador da via que recusou (→ USUARIO).</summary>
    public int IdUsuario { get; set; }
    /// <summary>quando a via recusou (a hora do campo).</summary>
    public DateTime Data { get; set; }
    /// <summary>por que a via não trocou.</summary>
    public string Motivo { get; set; } = "";
    /// <summary>o que a via viu.</summary>
    public string? Descricao { get; set; }
    /// <summary>o caminho da foto do local (obrigatória).</summary>
    public string FotoArquivo { get; set; } = "";
    /// <summary>a resposta do técnico: Manter ou Aceitar; nula enquanto ele não decide.</summary>
    public DecisaoRecusa? Decisao { get; set; }
    /// <summary>o técnico que decidiu (→ USUARIO).</summary>
    public int? IdUsuarioDecisao { get; set; }
    /// <summary>quando o técnico decidiu.</summary>
    public DateTime? DataDecisao { get; set; }
    /// <summary>Obrigatória para manter.</summary>
    public string? Resposta { get; set; }
    /// <summary>gerado no celular: o mesmo envio entra uma vez só.</summary>
    public Guid? IdCliente { get; set; }
    /// <summary>a hora do celular, quando a recusa foi feita.</summary>
    public DateTime RegistradoEm { get; set; }
    /// <summary>a hora em que chegou ao servidor.</summary>
    public DateTime? RecebidoEm { get; set; }
}