namespace ViaPermanente.Dominio.Models.Via;

/// <summary>DETECCAO_IMAGEM: uma linha por peça marcada num quadro da passagem; só insere.</summary>
public class DeteccaoImagem
{
    public int IdDeteccao { get; set; }
    public int IdPassagem { get; set; }
    /// <summary>Nulo se nenhuma tag respondeu no ponto.</summary>
    public int? IdDormente { get; set; }
    public decimal? KmMedido { get; set; }
    public string QuadroArquivo { get; set; } = "";
    public string? Camera { get; set; }
    public int CaixaX { get; set; }
    public int CaixaY { get; set; }
    public int CaixaLargura { get; set; }
    public int CaixaAltura { get; set; }
    public DefeitoImagem Defeito { get; set; }
    public byte Grau { get; set; }
    /// <summary>0 a 1.</summary>
    public decimal Confianca { get; set; }
    public string VersaoModelo { get; set; } = "";
    public DateTime Data { get; set; }
}