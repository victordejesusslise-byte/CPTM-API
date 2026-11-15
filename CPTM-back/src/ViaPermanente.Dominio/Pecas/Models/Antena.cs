
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>A antena usada pelo modo de leitura (MODO_LEITURA.antena).</summary>
public enum Antena
{
    /// <summary>Alcance curto, para ler só a peça de perto (modo Armazém).</summary>
    Curta,
    /// <summary>Alcance longo, para ler a metros (modo Via).</summary>
    Longa,
    /// <summary>Uma antena só (modos Normal e Personalizado).</summary>
    Unica
}
