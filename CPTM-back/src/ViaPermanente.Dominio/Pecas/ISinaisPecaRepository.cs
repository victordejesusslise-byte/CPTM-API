namespace ViaPermanente.Dominio.Pecas;

/// <summary>Busca, de uma vez, o que a nota e a posição de cada peça precisam.</summary>
public interface ISinaisPecaRepository
{
    Task<IReadOnlyDictionary<int, SinaisDaPeca>> Sinais(IReadOnlyCollection<int> idsDormente);

    /// <summary>As peças em uso: no armazém (numa pilha) ou assentadas na via, sem retirada.</summary>
    Task<IReadOnlyList<int>> PecasEmUso();
}
