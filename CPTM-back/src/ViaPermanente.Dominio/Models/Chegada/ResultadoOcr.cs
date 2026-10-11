namespace ViaPermanente.Dominio.Models.Chegada;

/// <summary>O que o motor de OCR devolve: o texto bruto, a confiança média (0 a 100) e as linhas.</summary>
public record ResultadoOcr(string Texto, decimal ConfiancaMedia, IReadOnlyList<LinhaOcr> Linhas);