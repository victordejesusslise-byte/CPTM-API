using ViaPermanente.Dominio.Comum;

namespace ViaPermanente.Dominio.Armazens.Models;

/// <summary>MOVIMENTACAO: só insere; o lugar de agora fica em DORMENTE.id_pilha, gravado junto.</summary>
public class Movimentacao : IRegistroDeCampo
{
    public int IdMovimentacao { get; set; }
    public int IdDormente { get; set; }
    public TipoMovimentacao Tipo { get; set; }
    public int? IdPilhaOrigem { get; set; }
    public int? IdPilhaDestino { get; set; }
    public int? IdTroca { get; set; }
    public int? IdLeitura { get; set; }
    public int IdUsuario { get; set; }
    public int? IdLeitor { get; set; }
    public DateTime Data { get; set; }
    public Guid? IdCliente { get; set; }
    public DateTime RegistradoEm { get; set; }
    public DateTime? RecebidoEm { get; set; }
}