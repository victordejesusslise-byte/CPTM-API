using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Chegadas.Dtos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Testes.Services;

/// <summary>A classe vem do pedido (Victor, 2026-10-16): o feixe fica com a classe do item do pedido que ele atende.</summary>
public class ClasseDoFeixeTestes
{
    private static NovoFeixeDto Feixe(int especie, int tamanho = 1, byte? classe = null) => new() { NumeroFeixe = "01", Quantidade = 10, IdEspecie = especie, IdTamanho = tamanho, Classe = classe };
    private static PedidoItem Item(byte classe, int? especie = null, int tamanho = 1) => new() { IdTamanho = tamanho, Classe = classe, IdEspecie = especie, Quantidade = 10 };

    [Fact]
    public void Vem_do_item_do_pedido_mesmo_que_seja_a_mesma_especie_em_classes_diferentes()
    {
        Assert.Equal(1, ChegadaService.ClasseDoFeixe(Feixe(3), [Item(1, 3), Item(2, 7)], "01"));
        Assert.Equal(2, ChegadaService.ClasseDoFeixe(Feixe(7), [Item(1, 3), Item(2, 7)], "01"));
        Assert.Equal(1, ChegadaService.ClasseDoFeixe(Feixe(5), [Item(1)], "01")); // item sem espécie: serve qualquer uma
    }

    [Fact]
    public void Com_1a_e_2a_para_o_mesmo_feixe_o_operador_escolhe()
    {
        PedidoItem[] pedido = [Item(1), Item(2)];
        Assert.Throws<RegraDeNegocioException>(() => ChegadaService.ClasseDoFeixe(Feixe(3), pedido, "01"));
        Assert.Equal(2, ChegadaService.ClasseDoFeixe(Feixe(3, classe: 2), pedido, "01"));
        Assert.Throws<RegraDeNegocioException>(() => ChegadaService.ClasseDoFeixe(Feixe(3, classe: 3), pedido, "01"));
    }
}
