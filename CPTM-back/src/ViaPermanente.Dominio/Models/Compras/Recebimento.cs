namespace ViaPermanente.Dominio.Models.Compras;

/// <summary>RECEBIMENTO: a chegada da carga num armazém; a primeira cria o LOTE.</summary>
public class Recebimento
{
    public int IdRecebimento { get; set; }
    public int IdPedido { get; set; }
    public int IdUsuario { get; set; }
    public int? IdNota { get; set; }
    public string? DocumentoOrigemTipo { get; set; }
    public string? DocumentoOrigemNumero { get; set; }
    public DateTime DataRecebimento { get; set; }
    public int QuantidadeRecebida { get; set; }
    public string? Observacao { get; set; }
    public int IdArmazem { get; set; }
}