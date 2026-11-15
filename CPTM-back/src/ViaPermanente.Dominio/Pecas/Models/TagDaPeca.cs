namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>Uma etiqueta da peça, lida da GRAVACAO_TAG (a linha Gravada ok ou Ligada).</summary>
/// <param name="IdGravacao">Id da linha da etiqueta (→ GRAVACAO_TAG).</param>
/// <param name="IdDormente">Id da peça dona da etiqueta (→ DORMENTE).</param>
/// <param name="Tag">O número da tag (BR e 10 dígitos).</param>
/// <param name="Vigente">Verdadeiro se é a que vale hoje; falso se já foi desligada.</param>
public record TagDaPeca(int IdGravacao, int IdDormente, string Tag, bool Vigente);
