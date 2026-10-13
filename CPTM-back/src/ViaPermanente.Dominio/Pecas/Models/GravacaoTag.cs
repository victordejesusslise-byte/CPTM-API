using ViaPermanente.Dominio.Comum;

namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>GRAVACAO_TAG: cada tentativa de gravar a tag; só insere.</summary>
public class GravacaoTag : IRegistroDeCampo
{
    public int IdGravacao { get; set; }
    public int? IdRfid { get; set; }
    public string TagGravada { get; set; } = "";
    public string? TagRelida { get; set; }
    public ResultadoGravacao Resultado { get; set; }
    public int TagsVistas { get; set; }
    public decimal? RssiDbm { get; set; }
    public int IdModo { get; set; }
    public int IdLeitor { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Data { get; set; }
    public Guid? IdCliente { get; set; }
    public DateTime RegistradoEm { get; set; }
    public DateTime? RecebidoEm { get; set; }
}