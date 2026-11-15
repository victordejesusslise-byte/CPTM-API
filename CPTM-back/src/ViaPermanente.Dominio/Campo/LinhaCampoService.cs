using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Pecas;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// Manda para o celular a cópia das peças que estão na via, em pedaços, para consultar e inspecionar sem rede.
/// A primeira vez vai tudo; depois, só o que mudou desde a última cópia (a peça que saiu da via vai como "Retirada").
/// </summary>
public class LinhaCampoService(IPecaRepository pecas, IInspecaoRepository inspecoes, PecaService consulta)
{
    /// <summary>o máximo de peças num pedaço.</summary>
    public const int LimiteMaximo = 5000;

    /// <summary>Um pedaço da cópia: as peças depois do id depoisDe, que mudaram desde "desde" (nulo: todas).</summary>
    public async Task<PaginaDaLinhaDto> Pagina(DateTime? desde, int depoisDe, int limite, DateTime agora)
    {
        // O tamanho do pedaço fica entre 1 e o máximo.
        limite = Math.Clamp(limite, 1, LimiteMaximo);
        var ids = await pecas.IdsDaLinha(desde, depoisDe, limite);
        var montadas = await consulta.Montar(await pecas.Resumos(ids: ids));
        var ultimas = await inspecoes.UltimasDasPecas(ids);
        // Cada peça vai com o resultado da última inspeção dela.
        var lista = montadas.Select(p =>
        {
            var ultima = ultimas.GetValueOrDefault(p.Id);
            return new PecaDaLinhaDto(p.Id, p.Codigo, p.Tag, p.KmProjeto, p.Posicao, p.IdTrecho, p.Situacao, p.Nota,
                ultima?.Resultado.ToString(), ultima?.FinalizadoEm);
        }).ToList();
        // Pedaço cheio: pode haver mais, o celular pede de novo a partir do último id.
        return new PaginaDaLinhaDto(lista, ids.Count == limite ? ids[^1] : null, agora);
    }
}
