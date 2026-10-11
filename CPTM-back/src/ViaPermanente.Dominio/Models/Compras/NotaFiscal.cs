namespace ViaPermanente.Dominio.Models.Compras;

/// <summary>NOTA_FISCAL: importada da planilha e conferida com o pedido.</summary>
public class NotaFiscal
{
    public int IdNota { get; set; }
    public int IdPedido { get; set; }
    public string Numero { get; set; } = "";
    public DateOnly DataEmissao { get; set; }
    public decimal? Valor { get; set; }
    public string? Arquivo { get; set; }
    public OrigemNota Origem { get; set; }
    public StatusConferenciaNota StatusConferencia { get; set; }
    public int? IdUsuarioConferiu { get; set; }
    public DateTime? DataConferencia { get; set; }
}