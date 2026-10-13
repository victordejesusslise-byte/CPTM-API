namespace ViaPermanente.Dominio.Chegadas.Models;

/// <summary>Uma linha de texto que o OCR leu, com a confiança (0 a 100) e onde está na foto (px).</summary>
public record LinhaOcr(string Texto, decimal Confianca, int X, int Y, int Largura, int Altura);