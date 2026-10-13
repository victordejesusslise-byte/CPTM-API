namespace ViaPermanente.Dominio.Via;

/// <summary>Um trecho com as estações das pontas.</summary>
public record TrechoDaVia(int IdTrecho, string Inicio, string Fim, string Linha, decimal KmInicio, decimal KmFim);

/// <summary>Os trechos e as peças assentadas neles (sem retirada), para a tela do trecho.</summary>
public interface ITrechoMapaRepository
{
    Task<IReadOnlyList<TrechoDaVia>> Trechos();
    Task<IReadOnlyList<(int IdDormente, int IdTrecho)>> PecasNaVia(int? idTrecho = null);
}
