using ViaPermanente.Dominio.Dtos.Ofertas;
using ViaPermanente.Dominio.Dtos.Pedidos;
using ViaPermanente.Dominio.Models.Compras;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Testes.Services;

public class OfertaServiceTestes
{
    private readonly OfertaRepositoryFalso ofertas = new();
    private OfertaService Service() => new(ofertas, new PedidoRepositoryFalso(), new CatalogoRepositoryFalso());

    private void Ofertar(int especie, decimal m3, int dia, int tamanho = 1)
        => ofertas.Linhas.Add(new OfertaFornecedor
        {
            IdOferta = ofertas.Linhas.Count + 1, IdEmpresa = 1, IdEspecie = especie, IdTamanho = tamanho,
            VolumeM3Disponivel = m3, Data = new DateOnly(2026, 10, dia)
        });

    [Fact]
    public async Task Vale_a_oferta_mais_recente()
    {
        Ofertar(1, 10m, dia: 1);
        Ofertar(1, 6m, dia: 5);

        var vigente = Assert.Single(await Service().ListarVigentes(1));
        Assert.Equal(6m, vigente.VolumeM3Disponivel);
    }

    [Fact]
    public async Task Pedido_acima_do_ofertado_avisa()
    {
        Ofertar(1, 6m, dia: 5);
        var avisos = await Service().Avisos(1, [new NovoPedidoItemDto { IdTamanho = 1, Classe = 1, IdEspecie = 1, Quantidade = 88 }]); // 10,05 m³

        var aviso = Assert.Single(avisos);
        Assert.Equal(6m, aviso.VolumeM3Ofertado);
        Assert.Contains("o fornecedor ofertou 6 m³", aviso.Mensagem);
    }

    [Fact]
    public async Task Sem_oferta_do_item_avisa()
        => Assert.Contains("não tem oferta de Ocotea neesiana de 2,80 m",
            Assert.Single(await Service().Avisos(1, [new NovoPedidoItemDto { IdTamanho = 1, Classe = 1, IdEspecie = 1, Quantidade = 5 }])).Mensagem);

    [Fact]
    public async Task Item_sem_especie_soma_a_oferta_da_classe()
    {
        Ofertar(1, 1m, dia: 5);
        Ofertar(14, 50m, dia: 5); // 2ª classe: não conta para item de 1ª
        var avisos = await Service().Avisos(1, [new NovoPedidoItemDto { IdTamanho = 1, Classe = 1, Quantidade = 5 }]); // 0,5712 m³

        Assert.Empty(avisos);
    }

    [Fact]
    public async Task So_fornecedor_registra_oferta()
        => await Assert.ThrowsAsync<RegraDeNegocioException>(() => Service().Registrar(new NovaOfertaDto
        {
            IdEmpresa = 2, IdEspecie = 1, IdTamanho = 1, VolumeM3Disponivel = 5m
        }));
}