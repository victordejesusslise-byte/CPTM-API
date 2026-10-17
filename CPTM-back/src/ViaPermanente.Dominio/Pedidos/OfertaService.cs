using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pedidos.Dtos;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Pedidos;

/// <summary>
/// A oferta do fornecedor: m³ disponível por espécie e tamanho. Cada oferta nova é uma linha (o histórico fica);
/// vale a mais recente. Ao montar o pedido, a oferta só avisa, não bloqueia (decisão do Victor, 2026-10-11).
/// </summary>
public class OfertaService(IOfertaRepository ofertas, IPedidoRepository pedidos, ICatalogoRepository catalogo)
{
    /// <summary>As ofertas que valem hoje; com idEmpresa, só as daquele fornecedor.</summary>
    public async Task<IReadOnlyList<OfertaDto>> ListarVigentes(int? idEmpresa)
    {
        var vigentes = Vigentes(await ofertas.Listar(idEmpresa));
        var especies = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie);
        var tamanhos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho);
        var empresas = await pedidos.NomesEmpresas(vigentes.Select(o => o.IdEmpresa).Distinct().ToList());

        return vigentes
            .Select(o => new OfertaDto(o.IdOferta, o.IdEmpresa, empresas.GetValueOrDefault(o.IdEmpresa, ""),
                o.IdEspecie, especies[o.IdEspecie].NomeCientifico,
                o.IdTamanho, tamanhos[o.IdTamanho].ComprimentoM, o.VolumeM3Disponivel, o.Data))
            .OrderBy(o => o.Empresa).ThenBy(o => o.Especie).ThenBy(o => o.ComprimentoM)
            .ToList();
    }

    public async Task<OfertaDto> Registrar(NovaOfertaDto nova)
    {
        var empresa = await pedidos.BuscarEmpresa(nova.IdEmpresa)
            ?? throw new RegraDeNegocioException("Fornecedor não encontrado.");
        if (empresa.Papel != PapelEmpresa.Fornecedor || !empresa.Ativo)
            throw new RegraDeNegocioException($"{empresa.Nome} não é um fornecedor ativo.");
        if ((await catalogo.ListarEspecies()).All(e => e.IdEspecie != nova.IdEspecie || !e.Ativo))
            throw new RegraDeNegocioException("Espécie não encontrada.");
        if ((await catalogo.ListarTamanhos()).All(t => t.IdTamanho != nova.IdTamanho || !t.Ativo))
            throw new RegraDeNegocioException("Tamanho de dormente não encontrado.");
        if (nova.VolumeM3Disponivel < 0)
            throw new RegraDeNegocioException("O volume disponível não pode ser negativo.");

        var oferta = new OfertaFornecedor
        {
            IdEmpresa = empresa.IdEmpresa, IdEspecie = nova.IdEspecie, IdTamanho = nova.IdTamanho,
            VolumeM3Disponivel = nova.VolumeM3Disponivel, Data = nova.Data ?? DateOnly.FromDateTime(DateTime.Now)
        };
        await ofertas.Adicionar(oferta);
        return (await ListarVigentes(empresa.IdEmpresa)).FirstOrDefault(o => o.Id == oferta.IdOferta)
            ?? throw new RegraDeNegocioException("Já existe uma oferta mais recente para esta espécie e este tamanho.");
    }

    /// <summary>
    /// Os avisos do pedido que está sendo montado: item que o fornecedor não oferta, ou m³ acima do ofertado.
    /// Item sem espécie conta a oferta de todas as espécies da mesma classe.
    /// </summary>
    public async Task<IReadOnlyList<AvisoOfertaDto>> Avisos(int idEmpresa, IReadOnlyList<NovoPedidoItemDto> itens)
    {
        var vigentes = Vigentes(await ofertas.Listar(idEmpresa));
        var especies = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie);
        var tamanhos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho);
        var avisos = new List<AvisoOfertaDto>();

        foreach (var grupo in itens.GroupBy(i => (i.IdTamanho, i.Classe, i.IdEspecie)))
        {
            if (!tamanhos.TryGetValue(grupo.Key.IdTamanho, out var tamanho))
                continue; // tamanho inválido é recusado pela regra do pedido
            var pedidoM3 = grupo.Sum(i => i.Quantidade) * VolumeHelper.M3(tamanho.ComprimentoM, tamanho.LarguraM, tamanho.AlturaM);
            // A oferta é por espécie e tamanho; a classe vem do pedido, então qualquer espécie serve à 1ª ou à 2ª.
            var candidatas = vigentes.Where(o => o.IdTamanho == tamanho.IdTamanho && (grupo.Key.IdEspecie is not int idEspecie || o.IdEspecie == idEspecie)).ToList();
            var oQue = grupo.Key.IdEspecie is int id && especies.TryGetValue(id, out var especie)
                ? $"{especie.NomeCientifico} de {FormatoHelper.Metros(tamanho.ComprimentoM)}"
                : $"{FormatoHelper.Metros(tamanho.ComprimentoM)} de {grupo.Key.Classe}ª classe";

            if (candidatas.Count == 0)
                avisos.Add(new AvisoOfertaDto(tamanho.IdTamanho, grupo.Key.Classe, grupo.Key.IdEspecie, pedidoM3, null,
                    $"O fornecedor não tem oferta de {oQue}."));
            else if (pedidoM3 > candidatas.Sum(o => o.VolumeM3Disponivel))
            {
                var ofertado = candidatas.Sum(o => o.VolumeM3Disponivel);
                avisos.Add(new AvisoOfertaDto(tamanho.IdTamanho, grupo.Key.Classe, grupo.Key.IdEspecie, pedidoM3, ofertado,
                    $"O pedido de {oQue} soma {FormatoHelper.MetrosCubicos(pedidoM3)} e o fornecedor ofertou {FormatoHelper.MetrosCubicos(ofertado)}."));
            }
        }
        return avisos;
    }

    /// <summary>A oferta que vale: a de data mais recente (e, no mesmo dia, a última registrada) por fornecedor, espécie e tamanho.</summary>
    public static IReadOnlyList<OfertaFornecedor> Vigentes(IEnumerable<OfertaFornecedor> todas)
        => todas.GroupBy(o => (o.IdEmpresa, o.IdEspecie, o.IdTamanho))
            .Select(g => g.OrderByDescending(o => o.Data).ThenByDescending(o => o.IdOferta).First())
            .ToList();
}