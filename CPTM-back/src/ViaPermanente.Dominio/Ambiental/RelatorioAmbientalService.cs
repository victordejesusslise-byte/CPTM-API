using ViaPermanente.Dominio.Ambiental.Dtos;
using ViaPermanente.Dominio.Ambiental.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Comum;

namespace ViaPermanente.Dominio.Ambiental;

/// <summary>
/// O relatório ambiental mensal (estoque por espécie em m³ e tempo). O rascunho pode ser gerado de novo;
/// depois de enviado, não muda (Relatório ambiental congelado ao enviar).
/// </summary>
public class RelatorioAmbientalService(IRelatorioAmbientalRepository relatorios, ICatalogoRepository catalogo)
{
    public async Task<RelatorioDto> Gerar(DateOnly mes, int idUsuario)
    {
        var referencia = new DateOnly(mes.Year, mes.Month, 1);
        var doMes = await relatorios.Listar(mes: referencia);
        if (doMes.Any(r => r.Status == StatusRelatorio.Enviado))
            throw new RegraDeNegocioException($"O relatório de {referencia:MM/yyyy} já foi enviado e não muda mais.");
        var relatorio = new RelatorioAmbiental { MesReferencia = referencia, GeradoEm = DateTime.Now, IdUsuario = idUsuario, Status = StatusRelatorio.Rascunho };
        await relatorios.Gravar(relatorio, RelatorioAmbientalCalculo.Itens(referencia, await relatorios.Pecas()), doMes.FirstOrDefault()?.IdRelatorio);
        return (await BuscarPorId(relatorio.IdRelatorio))!;
    }

    /// <summary>Envia o rascunho: daqui em diante, correções nos dados não mudam o que foi enviado. Nulo se não existe.</summary>
    public async Task<RelatorioDto?> Enviar(int idRelatorio)
    {
        var relatorio = (await relatorios.Listar(idRelatorio)).FirstOrDefault();
        if (relatorio is null)
            return null;
        if (relatorio.Status == StatusRelatorio.Enviado)
            throw new RegraDeNegocioException("O relatório já foi enviado.");
        await relatorios.Enviar(idRelatorio, DateTime.Now);
        return await BuscarPorId(idRelatorio);
    }

    public async Task<IReadOnlyList<RelatorioDto>> Listar() => await Montar(await relatorios.Listar());

    public async Task<RelatorioDto?> BuscarPorId(int idRelatorio) => (await Montar(await relatorios.Listar(idRelatorio))).FirstOrDefault();

    private async Task<IReadOnlyList<RelatorioDto>> Montar(IReadOnlyList<RelatorioAmbiental> lista)
    {
        if (lista.Count == 0)
            return [];
        var itens = (await relatorios.Itens(lista.Select(r => r.IdRelatorio).ToList())).ToLookup(i => i.IdRelatorio);
        var especies = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie, e => e.NomeCientifico);
        return lista.Select(r =>
        {
            var dele = itens[r.IdRelatorio].Select(i => new ItemRelatorioDto(i.IdEspecie, especies.GetValueOrDefault(i.IdEspecie, ""),
                i.VolumeM3Estoque, i.VolumeM3Entrada, i.VolumeM3Saida, i.IdadeMediaDias)).ToList();
            return new RelatorioDto(r.IdRelatorio, r.MesReferencia, r.Status.ToString(), r.GeradoEm, r.EnviadoEm, dele.Sum(i => i.VolumeM3Estoque), dele);
        }).ToList();
    }
}