using ViaPermanente.Dominio.Comum;

namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>GRAVACAO_TAG: o histórico da etiqueta (tag) de cada peça: gravada, ligada em campo ou desligada.
/// Só se acrescentam linhas; a etiqueta que vale hoje é a marcada como vigente, uma por peça
/// (decisão: Etiqueta na GRAVACAO_TAG, sem a RFID).</summary>
public class GravacaoTag : IRegistroDeCampo
{
    /// <summary>Id da linha.</summary>
    public int IdGravacao { get; set; }
    /// <summary>Gravada, Ligada ou Desligada.</summary>
    public TipoGravacao Tipo { get; set; } = TipoGravacao.Gravada;
    /// <summary>Id da peça (→ DORMENTE). Nulo só na gravação que falhou antes de a peça existir.</summary>
    public int? IdDormente { get; set; }
    /// <summary>O número da tag (BR e 10 dígitos, ex.: BR0000000001).</summary>
    public string TagGravada { get; set; } = "";
    /// <summary>O número que o módulo leu de volta depois de gravar.</summary>
    public string? TagRelida { get; set; }
    /// <summary>Se a gravação deu certo: ok, divergente, bloqueada ou falhou. Na Ligada e na Desligada, sempre ok.</summary>
    public ResultadoGravacao Resultado { get; set; }
    /// <summary>Quantas tags o módulo viu na hora de gravar.</summary>
    public int TagsVistas { get; set; }
    /// <summary>A força do sinal da tag, em dBm.</summary>
    public decimal? RssiDbm { get; set; }
    /// <summary>Verdadeiro na etiqueta que vale hoje para a peça.</summary>
    public bool Vigente { get; set; }
    /// <summary>O motivo da Ligada ou da Desligada, ex.: "peça reprovada".</summary>
    public string? Observacao { get; set; }
    /// <summary>Id do modo de leitura (→ MODO_LEITURA). Nulo na Desligada.</summary>
    public int? IdModo { get; set; }
    /// <summary>Id do leitor usado (→ LEITOR). Nulo na Desligada.</summary>
    public int? IdLeitor { get; set; }
    /// <summary>Id de quem fez (→ USUARIO).</summary>
    public int IdUsuario { get; set; }
    /// <summary>Quando foi feita.</summary>
    public DateTime Data { get; set; }
    /// <summary>Gerado no celular: o mesmo envio repetido entra uma vez.</summary>
    public Guid? IdCliente { get; set; }
    /// <summary>A hora do celular; sem ela, a do servidor.</summary>
    public DateTime RegistradoEm { get; set; }
    /// <summary>A hora em que chegou ao servidor.</summary>
    public DateTime? RecebidoEm { get; set; }

    /// <summary>Monta a linha Desligada de uma etiqueta que deixou de valer (sem leitor e não vigente).</summary>
    public static GravacaoTag Desligada(int idDormente, string tag, string observacao, int idUsuario, DateTime agora) => new()
    {
        Tipo = TipoGravacao.Desligada, IdDormente = idDormente, TagGravada = tag, Resultado = ResultadoGravacao.Ok,
        Observacao = observacao, IdUsuario = idUsuario, Data = agora, RegistradoEm = agora, RecebidoEm = agora
    };
}
