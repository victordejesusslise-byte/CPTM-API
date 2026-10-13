namespace ViaPermanente.Dominio.Laudos.Models;

/// <summary>O conteúdo de um laudo, sem saber de PDF: quem desenha é o IDocumentoPdfRepository.</summary>
public record DocumentoPdf(string Titulo, string Codigo, string Subtitulo, DateTime GeradoEm, IReadOnlyList<SecaoPdf> Secoes);