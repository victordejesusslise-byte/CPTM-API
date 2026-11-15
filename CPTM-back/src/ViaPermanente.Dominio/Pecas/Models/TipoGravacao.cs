namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>O tipo de cada linha do histórico da etiqueta (GRAVACAO_TAG).</summary>
public enum TipoGravacao
{
    /// <summary>O módulo gravou o número numa tag nova, no cadastro.</summary>
    Gravada,
    /// <summary>O técnico ou o supervisor ligou à peça uma tag achada em campo.</summary>
    Ligada,
    /// <summary>A tag deixou de valer (ex.: peça reprovada ou tag trocada).</summary>
    Desligada
}
