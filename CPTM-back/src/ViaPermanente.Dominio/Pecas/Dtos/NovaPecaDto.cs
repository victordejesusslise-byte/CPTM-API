using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>
/// O cadastro da peça: o feixe de onde ela veio, a identificação do fornecedor (não é UHF) e a nossa tag UHF.
/// Com o módulo: o número gravado, o relido e quantas tags estavam no alcance. Com a pistola: a tag lida.
/// </summary>
public class NovaPecaDto
{
    [Range(1, int.MaxValue)] public int IdFeixe { get; init; }
    public string? TagFornecedor { get; init; }
    /// <summary>Câmera, NFC ou digitado.</summary>
    public MetodoCaptura? TagFornecedorLeitura { get; init; }
    /// <summary>Sempre ModuloUhf: só o módulo do celular crava a tag (a pistola agora é a NIR).</summary>
    public MetodoCaptura Metodo { get; init; } = MetodoCaptura.ModuloUhf;
    /// <summary>O número gravado pelo módulo (peça em GET /api/tags/proxima) ou lido pela pistola.</summary>
    [Required] public string Tag { get; init; } = "";
    /// <summary>Módulo: o que a releitura trouxe.</summary>
    public string? TagRelida { get; init; }
    public int TagsVistas { get; init; } = 1;
    public decimal? RssiDbm { get; init; }
    public int? IdLeitor { get; init; }
    public int? IdModo { get; init; }
    /// <summary>Gerado no celular: o mesmo envio repetido entra uma vez.</summary>
    public Guid? IdCliente { get; init; }
    /// <summary>A hora do celular; sem ela, vale a do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}