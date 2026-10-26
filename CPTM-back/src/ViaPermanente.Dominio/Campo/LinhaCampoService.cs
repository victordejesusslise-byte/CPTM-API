using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Pecas;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// A cópia das linhas para o celular (app, Entrega 2, 2026-10-25): as peças cadastradas que estão na via, em pedaços por id, para o
/// celular consultar e inspecionar qualquer peça sem rede. A primeira vez vai tudo; depois, só o que mudou "desde" (DORMENTE.data_alteracao),
/// e aí as peças que saíram da via vão como "Retirada", para o celular apagar.
/// </summary>
public class LinhaCampoService(IPecaRepository pecas, IInspecaoRepository inspecoes, PecaService consulta)
{
    public const int LimiteMaximo = 5000;

    public async Task<PaginaDaLinhaDto> Pagina(DateTime? desde, int depoisDe, int limite, DateTime agora)
    {
        limite = Math.Clamp(limite, 1, LimiteMaximo);
        var ids = await pecas.IdsDaLinha(desde, depoisDe, limite);
        var montadas = await consulta.Montar(await pecas.Resumos(ids: ids));
        var ultimas = await inspecoes.UltimasDasPecas(ids);
        var lista = montadas.Select(p =>
        {
            var ultima = ultimas.GetValueOrDefault(p.Id);
            return new PecaDaLinhaDto(p.Id, p.Codigo, p.Tag, p.KmProjeto, p.Posicao, p.IdTrecho, p.Situacao, p.Nota,
                ultima?.Resultado.ToString(), ultima?.FinalizadoEm);
        }).ToList();
        return new PaginaDaLinhaDto(lista, ids.Count == limite ? ids[^1] : null, agora);
    }
}
