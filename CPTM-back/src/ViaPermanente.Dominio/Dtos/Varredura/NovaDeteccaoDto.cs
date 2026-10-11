using ViaPermanente.Dominio.Models.Via;

namespace ViaPermanente.Dominio.Dtos.Varredura;

/// <summary>Uma peça marcada pela câmera num quadro. A tag, se respondeu no mesmo ponto, dá a peça.</summary>
public class NovaDeteccaoDto
{
    public string? Tag { get; init; }
    public decimal? KmMedido { get; init; }
    public string QuadroArquivo { get; init; } = "";
    public string? Camera { get; init; }
    public int CaixaX { get; init; }
    public int CaixaY { get; init; }
    public int CaixaLargura { get; init; }
    public int CaixaAltura { get; init; }
    public DefeitoImagem Defeito { get; init; }
    public byte Grau { get; init; } = 1;
    /// <summary>0 a 1.</summary>
    public decimal Confianca { get; init; }
    public string VersaoModelo { get; init; } = "";
    public DateTime Data { get; init; }
}