
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>O resultado de gravar a tag. Ok só quando a releitura bate e havia uma tag só.</summary>
public enum ResultadoGravacao
{
    /// <summary>Gravou e a releitura bateu: a tag vale para a peça.</summary>
    Ok,
    /// <summary>A releitura trouxe outro número: a tag não vale, e o número não é usado de novo.</summary>
    Divergente,
    /// <summary>Havia mais de uma tag no alcance: nada foi gravado.</summary>
    Bloqueada,
    /// <summary>A releitura não trouxe número: pode tentar de novo com o mesmo número.</summary>
    Falhou
}
