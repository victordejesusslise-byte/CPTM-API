namespace ViaPermanente.Dominio.Laudos.Models;

/// <summary>
/// Uma foto no laudo (Victor, 2026-10-25: "a imagem tem que aparecer no laudo"): a legenda, o caminho na pasta das fotos, o tipo
/// (image/jpeg, image/png…) e a imagem. Conteúdo nulo: o arquivo não está no servidor (o laudo diz isso no lugar da foto).
/// </summary>
public record FotoPdf(string Legenda, string Caminho, string? Tipo, byte[]? Conteudo);
