namespace ViaPermanente.Dominio.Helpers;

/// <summary>O m³ do dormente é calculado, não guardado.</summary>
public static class VolumeHelper
{
    /// <summary>Comprimento × largura × altura, em m³, com 5 casas (2,80 × 0,24 × 0,17 = 0,11424).</summary>
    public static decimal M3(decimal comprimentoM, decimal larguraM, decimal alturaM)
        => Math.Round(comprimentoM * larguraM * alturaM, 5);
}