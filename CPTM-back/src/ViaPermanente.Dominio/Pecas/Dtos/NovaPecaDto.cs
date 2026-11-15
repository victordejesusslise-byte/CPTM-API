using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>O cadastro da peça: o feixe de onde ela veio, a identificação do fornecedor (não é UHF) e a nossa tag UHF.
/// O módulo manda o número gravado, o relido e quantas tags estavam no alcance.</summary>
public class NovaPecaDto
{
    /// <summary>Id do feixe de onde a peça veio (→ FEIXE).</summary>
    [Range(1, int.MaxValue)] public int IdFeixe { get; init; }
    /// <summary>A identificação do fornecedor, ex.: TG-20261013193233-28.</summary>
    public string? TagFornecedor { get; init; }
    /// <summary>Câmera, NFC ou digitado.</summary>
    public MetodoCaptura? TagFornecedorLeitura { get; init; }
    /// <summary>Sempre ModuloUhf: só o módulo do celular crava a tag (a pistola agora é a NIR).</summary>
    public MetodoCaptura Metodo { get; init; } = MetodoCaptura.ModuloUhf;
    /// <summary>O número gravado pelo módulo (BR e 10 dígitos; pegue em GET /api/tags/proxima).</summary>
    [Required] public string Tag { get; init; } = "";
    /// <summary>Módulo: o que a releitura trouxe.</summary>
    public string? TagRelida { get; init; }
    /// <summary>Quantas tags o módulo viu na hora de gravar; tem de ser 1.</summary>
    public int TagsVistas { get; init; } = 1;
    /// <summary>A força do sinal da tag, em dBm.</summary>
    public decimal? RssiDbm { get; init; }
    /// <summary>Id do leitor (→ LEITOR). Nulo: vale o único módulo ativo cadastrado.</summary>
    public int? IdLeitor { get; init; }
    /// <summary>Id do modo de leitura (→ MODO_LEITURA). Obrigatório com o módulo.</summary>
    public int? IdModo { get; init; }
    /// <summary>Gerado no celular: o mesmo envio repetido entra uma vez.</summary>
    public Guid? IdCliente { get; init; }
    /// <summary>A hora do celular; sem ela, vale a do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}