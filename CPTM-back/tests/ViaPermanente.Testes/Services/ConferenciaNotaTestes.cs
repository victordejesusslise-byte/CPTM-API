using ViaPermanente.Dominio.Models.Catalogos;
using ViaPermanente.Dominio.Models.Compras;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Testes.Services;

/// <summary>Pedido de 28 peças de 2,80 m de 1ª (2,80 m = 0,11424 m³ por peça).</summary>
public class ConferenciaNotaTestes
{
    private static readonly Dictionary<int, TamanhoDormente> Tamanhos = new()
    {
        [1] = new() { IdTamanho = 1, ComprimentoM = 2.80m, LarguraM = 0.24m, AlturaM = 0.17m, MedidaProvisoria = true, Ativo = true },
        [2] = new() { IdTamanho = 2, ComprimentoM = 3.00m, LarguraM = 0.24m, AlturaM = 0.17m, MedidaProvisoria = false, Ativo = true }
    };
    private static readonly Dictionary<int, Especie> Especies = new()
    {
        [1] = new() { IdEspecie = 1, NomeCientifico = "Ocotea neesiana", Classe = 1, Ativo = true },
        [3] = new() { IdEspecie = 3, NomeCientifico = "Hymenaea courbaril", Classe = 1, Ativo = true },
        [14] = new() { IdEspecie = 14, NomeCientifico = "Eucalyptus grandis", Classe = 2, Ativo = true }
    };
    private static readonly PedidoItem[] Pedido = [new() { IdTamanho = 1, Classe = 1, Quantidade = 28 }];

    private static NotaFiscalItem Item(int especie, int quantidade, decimal? m3 = null, int tamanho = 1, byte classe = 1)
        => new() { IdEspecie = especie, IdTamanho = tamanho, Classe = classe, Quantidade = quantidade, VolumeM3 = m3 ?? quantidade * 0.11424m };

    private static ResultadoConferencia Conferir(IReadOnlyList<NotaFiscalItem> nota, IReadOnlyList<NotaFiscalItem>? anteriores = null, IReadOnlyList<PedidoItem>? pedido = null)
        => ConferenciaNota.Conferir(pedido ?? Pedido, anteriores ?? [], nota, Tamanhos, Especies, 0.02m);

    [Fact]
    public void Nota_parcial_confere_e_avisa_o_que_falta()
    {
        var r = Conferir([Item(1, 14)]);
        Assert.True(r.Confere);
        Assert.Contains(r.Avisos, a => a.Contains("faltam 14 peças"));
    }

    [Fact]
    public void Mais_pecas_do_que_falta_diverge()
    {
        var r = Conferir([Item(3, 20)], anteriores: [Item(1, 14)]);
        Assert.False(r.Confere);
        Assert.Contains(r.Divergencias, d => d.Contains("a nota traz 20 peças e faltam 14"));
    }

    [Fact]
    public void Tamanho_ou_classe_que_nao_foi_pedido_diverge()
        => Assert.Contains("3,00 m de 1ª classe não foi pedido.", Conferir([Item(1, 5, 5 * 0.12240m, tamanho: 2)]).Divergencias);

    [Fact]
    public void Especie_diferente_da_pedida_diverge()
    {
        PedidoItem[] soOcotea = [new() { IdTamanho = 1, Classe = 1, IdEspecie = 1, Quantidade = 28 }];
        Assert.Contains(Conferir([Item(3, 14)], pedido: soOcotea).Divergencias, d => d.Contains("Hymenaea courbaril não foi pedida"));
    }

    [Fact]
    public void Classe_diferente_da_especie_diverge()
        => Assert.Contains(Conferir([Item(14, 5)]).Divergencias, d => d.Contains("Eucalyptus grandis é de 2ª classe"));

    [Fact]
    public void Volume_com_medida_provisoria_so_avisa()
    {
        var r = Conferir([Item(1, 28, 3.25m)]); // catálogo: 3,19872 (1,6%, abaixo da tolerância, mas a medida é provisória)
        Assert.True(r.Confere);
        Assert.Contains(r.Avisos, a => a.Contains("medida do catálogo ainda é provisória"));
    }

    [Fact]
    public void Volume_com_medida_real_acima_da_tolerancia_diverge()
    {
        PedidoItem[] pedido300 = [new() { IdTamanho = 2, Classe = 1, Quantidade = 10 }];
        var r = Conferir([Item(1, 10, 1.30m, tamanho: 2)], pedido: pedido300); // catálogo: 1,224
        Assert.False(r.Confere);
        Assert.Contains(r.Divergencias, d => d.Contains("a nota declara 1,3 m³ e o catálogo calcula 1,224 m³"));
    }

    [Fact]
    public void Volume_com_medida_real_dentro_da_tolerancia_confere_sem_aviso()
    {
        PedidoItem[] pedido300 = [new() { IdTamanho = 2, Classe = 1, Quantidade = 10 }];
        var r = Conferir([Item(1, 10, 1.23m, tamanho: 2)], pedido: pedido300); // catálogo: 1,224 (0,5%)
        Assert.True(r.Confere);
        Assert.DoesNotContain(r.Avisos, a => a.Contains("m³"));
    }
}