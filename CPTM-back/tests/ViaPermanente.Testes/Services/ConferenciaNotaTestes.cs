using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Testes.Services;

/// <summary>Pedido de 28 peças de 2,80 m de 1ª (2,80 m = 0,11424 m³ por peça).</summary>
public class ConferenciaNotaTestes
{
    private static readonly Dictionary<int, TamanhoDormente> Tamanhos = TamanhoDormente.Todos.ToDictionary(t => t.IdTamanho);
    /// <summary>Os padrões do fornecedor: 1 para todas as espécies (0,24 × 0,17) e 2 do Eucalyptus (0,22 × 0,16).</summary>
    private static readonly Dictionary<int, MedidaFornecedor> Medidas = new()
    {
        [1] = new() { IdMedida = 1, IdEmpresa = 1, LarguraM = 0.24m, AlturaM = 0.17m },
        [2] = new() { IdMedida = 2, IdEmpresa = 1, IdEspecie = 14, LarguraM = 0.22m, AlturaM = 0.16m }
    };
    private static readonly Dictionary<int, Especie> Especies = new()
    {
        [1] = new() { IdEspecie = 1, NomeCientifico = "Ocotea neesiana", Ativo = true },
        [3] = new() { IdEspecie = 3, NomeCientifico = "Hymenaea courbaril", Ativo = true },
        [14] = new() { IdEspecie = 14, NomeCientifico = "Eucalyptus grandis", Ativo = true }
    };
    private static readonly PedidoItem[] Pedido = [new() { IdTamanho = 1, Classe = 1, Quantidade = 28, IdMedida = 1 }];

    private static NotaFiscalItem Item(int especie, int quantidade, decimal? m3 = null, int tamanho = 1, byte classe = 1)
        => new() { IdEspecie = especie, IdTamanho = tamanho, Classe = classe, Quantidade = quantidade, VolumeM3 = m3 ?? quantidade * 0.11424m };

    private static ResultadoConferencia Conferir(IReadOnlyList<NotaFiscalItem> nota, IReadOnlyList<NotaFiscalItem>? anteriores = null, IReadOnlyList<PedidoItem>? pedido = null)
        => ConferenciaNota.Conferir(pedido ?? Pedido, anteriores ?? [], nota, Tamanhos, Especies, Medidas, 0.02m);

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
        PedidoItem[] soOcotea = [new() { IdTamanho = 1, Classe = 1, IdEspecie = 1, Quantidade = 28, IdMedida = 1 }];
        Assert.Contains(Conferir([Item(3, 14)], pedido: soOcotea).Divergencias, d => d.Contains("Hymenaea courbaril não foi pedida"));
    }

    [Fact]
    public void Qualquer_especie_pode_vir_de_1a_ou_de_2a()
        => Assert.DoesNotContain(Conferir([Item(14, 5)]).Divergencias, d => d.Contains("ª classe, mas a nota"));

    [Fact]
    public void Volume_abaixo_da_tolerancia_confere()
    {
        var r = Conferir([Item(1, 28, 3.25m)]); // padrão: 3,19872 (1,6%, abaixo da tolerância de 2%)
        Assert.True(r.Confere);
        Assert.DoesNotContain(r.Divergencias, d => d.Contains("m³"));
    }

    [Fact]
    public void O_volume_usa_o_padrao_do_item_do_pedido()
    {
        // O Eucalyptus foi pedido no padrão dele (0,22 × 0,16): 10 × 2,80 × 0,22 × 0,16 = 0,9856 m³.
        PedidoItem[] eucalipto = [new() { IdTamanho = 1, Classe = 1, IdEspecie = 14, Quantidade = 10, IdMedida = 2 }];
        Assert.True(Conferir([Item(14, 10, 0.9856m)], pedido: eucalipto).Confere);
        var r = Conferir([Item(14, 10, 1.14240m)], pedido: eucalipto); // o volume do padrão 0,24 × 0,17
        Assert.False(r.Confere);
        Assert.Contains(r.Divergencias, d => d.Contains("padrão do fornecedor (0,22 × 0,16 m) dá 0,986 m³"));
    }

    [Fact]
    public void Volume_acima_da_tolerancia_diverge()
    {
        PedidoItem[] pedido300 = [new() { IdTamanho = 2, Classe = 1, Quantidade = 10, IdMedida = 1 }];
        var r = Conferir([Item(1, 10, 1.30m, tamanho: 2)], pedido: pedido300); // catálogo: 1,224
        Assert.False(r.Confere);
        Assert.Contains(r.Divergencias, d => d.Contains("a nota declara 1,3 m³ e o padrão do fornecedor (0,24 × 0,17 m) dá 1,224 m³"));
    }

    [Fact]
    public void Volume_dentro_da_tolerancia_confere_sem_aviso()
    {
        PedidoItem[] pedido300 = [new() { IdTamanho = 2, Classe = 1, Quantidade = 10, IdMedida = 1 }];
        var r = Conferir([Item(1, 10, 1.23m, tamanho: 2)], pedido: pedido300); // catálogo: 1,224 (0,5%)
        Assert.True(r.Confere);
        Assert.DoesNotContain(r.Avisos, a => a.Contains("m³"));
    }
}