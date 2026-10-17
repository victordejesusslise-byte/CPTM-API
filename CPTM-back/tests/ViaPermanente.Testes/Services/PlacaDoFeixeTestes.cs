using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Testes.Services;

public class PlacaDoFeixeTestes
{
    private static LinhaOcr L(string texto, decimal confianca = 95) => new(texto, confianca, 0, 0, 100, 20);
    private static readonly PedidoParaPlaca Pedido = new("TG–Três Guri", "AR00224", "AR00224-03", [("Ocotea neesiana", 1)], [1], [2.80m]);

    [Fact]
    public void Acha_os_nove_campos_da_placa()
    {
        var campos = PlacaDoFeixe.Extrair([L("Fornecedor: TG - Três Guri"), L("Ata de registro: AR00224"), L("Essência / classe: Ocotea neesiana, Lauraceae / 1ª"),
            L("Comprimento: 2,80 m"), L("Tratamento / empresa: CCA / Granoski"), L("Empresa inspetora / ano: UFV / 2025"), L("Contrato: AR00224-03"), L("linha sem rótulo")]);

        Assert.Equal(9, campos.Count);
        Assert.Equal("Granoski", campos.Single(c => c.Campo == CampoOcr.Tratadora).Valor);
        Assert.Equal("2025", campos.Single(c => c.Campo == CampoOcr.Ano).Valor);
    }

    private static Dictionary<CampoOcr, string> Valores(IReadOnlyList<CampoLido> campos) => campos.ToDictionary(c => c.Campo, c => c.Valor);

    /// <summary>Linhas que o Tesseract leu, célula por célula, da foto da placa real: lixo em volta e o valor da célula em duas linhas.</summary>
    [Fact]
    public void Placa_real_lida_celula_por_celula()
    {
        var campos = Valores(PlacaDoFeixe.Extrair([L("| Der"), L("* TG-TRÊSGURI |"), L("ATA DE IST: ] AR00224"),
            L("— ESSÊNCIA/\nPE COLASSE “=: * OCOTEA NEESIANA, |\nLAURACEAE / la"), L("COMPRIMENTO: 2,80 M"),
            L("\"TRATAMENTO\n\"* [EMPRESA: | CCA\n* [GRANOSKI"), L("— EMPRESA\n— INSP./ANO -: UFV /\n2025 )"), L("a ii ce\nCONTRATO ê: | AR00224-03")]));

        Assert.Equal("AR00224", campos[CampoOcr.Ata]);
        Assert.Equal("OCOTEA NEESIANA, LAURACEAE / 1a", campos[CampoOcr.EspecieClasse]);
        Assert.Equal("2,80 M", campos[CampoOcr.Comprimento]);
        Assert.Equal("CCA", campos[CampoOcr.Tratamento]);
        Assert.Equal("GRANOSKI", campos[CampoOcr.Tratadora]);
        Assert.Equal("UFV", campos[CampoOcr.Inspetora]);
        Assert.Equal("2025", campos[CampoOcr.Ano]);
        Assert.Equal("AR00224-03", campos[CampoOcr.Contrato]);
        Assert.False(campos.ContainsKey(CampoOcr.Fornecedor)); // sem rótulo: só com o pedido (AcharFornecedor)
        Assert.True(PlacaDoFeixe.Confere(CampoOcr.EspecieClasse, campos[CampoOcr.EspecieClasse], Pedido));
    }

    [Fact]
    public void Celula_de_rotulo_sozinha_nao_vira_valor()
    {
        Assert.Empty(PlacaDoFeixe.Extrair([L("— ESSÊNCIA/\nER ECIASSE"), L("| CCA\n* [GRANOSKI")]));
        Assert.Null(PlacaDoFeixe.Confere(CampoOcr.EspecieClasse, "Ocotea neesiana", Pedido with { Especies = [] }));
    }

    /// <summary>A foto inteira lida de uma vez: sem dois-pontos, com o rótulo e o valor partidos em duas linhas.</summary>
    [Fact]
    public void Placa_real_lida_inteira_sem_dois_pontos()
    {
        var campos = Valores(PlacaDoFeixe.Extrair([L("ATA DE REGISTRO) AR00224 À"), L("ESSÊNCIA/ OCOTEA NEESIANA, |"), L("CLASSE LAURACEAE / la"),
            L("2 [E TRATAMENTO CCA o |"), L("/ EMPRESA / GRANOSKI"), L("ds EMPRESA UFV / | ao"), L(">. * INSP./ANO 2025 1» [e"), L(">» a! CONTRATO QB AR00224-03")]));

        Assert.Equal("AR00224", campos[CampoOcr.Ata]);
        Assert.Equal("OCOTEA NEESIANA, LAURACEAE / 1a", campos[CampoOcr.EspecieClasse]);
        Assert.Equal("CCA", campos[CampoOcr.Tratamento]);
        Assert.Equal("GRANOSKI", campos[CampoOcr.Tratadora]);
        Assert.Equal("UFV", campos[CampoOcr.Inspetora]);
        Assert.Equal("2025", campos[CampoOcr.Ano]);
        Assert.Equal("AR00224-03", campos[CampoOcr.Contrato]);
    }

    [Fact]
    public void Pedacos_da_mesma_faixa_viram_rotulo_e_valor()
    {
        var faixas = PlacaDoFeixe.EmFaixas([new("AR00224", 83, 462, 520, 143, 27), new("ATA DE REGISTRO", 82, 147, 520, 237, 24),
            new("INSP. / ANO", 91, 184, 902, 172, 32), new("2025", 97, 501, 902, 68, 25)]);

        Assert.Equal(["ATA DE REGISTRO: AR00224", "INSP. / ANO: 2025"], faixas.Select(f => f.Texto));
        Assert.Equal((147, 520), (faixas[0].X, faixas[0].Y));
        Assert.Equal(82, faixas[0].Confianca);
    }

    [Fact]
    public void Acha_o_fornecedor_do_pedido_mesmo_com_erro_de_leitura()
    {
        Assert.Equal("TG-TRÊSGURI", PlacaDoFeixe.AcharFornecedor([L("| Der"), L("* TG-TRÊSGURI |")], "TG–Três Guri")?.Valor);
        Assert.Equal("TG-TRES GURL", PlacaDoFeixe.AcharFornecedor([L("TG-TRES GURL")], "TG–Três Guri")?.Valor);
        Assert.Null(PlacaDoFeixe.AcharFornecedor([L("CR ENA GUIN"), L("ATA DE REGISTRO")], "TG–Três Guri"));
        Assert.True(PlacaDoFeixe.Confere(CampoOcr.Fornecedor, "TG-TRES GURL", Pedido));
    }

    [Theory]
    [InlineData(CampoOcr.Fornecedor, "TG - Três Guri", true)]
    [InlineData(CampoOcr.Fornecedor, "Outra Madeireira", false)]
    [InlineData(CampoOcr.Contrato, "AR00224-03", true)]
    [InlineData(CampoOcr.Contrato, "AR00224-04", false)]
    [InlineData(CampoOcr.EspecieClasse, "Ocotea neesiana, Lauraceae / 1ª", true)]
    [InlineData(CampoOcr.EspecieClasse, "Ocotea neesiana, Lauraceae / 2ª", false)]
    [InlineData(CampoOcr.Comprimento, "2,80 m", true)]
    [InlineData(CampoOcr.Comprimento, "3,00 m", false)]
    public void Confere_com_o_pedido(CampoOcr campo, string valor, bool esperado) => Assert.Equal(esperado, PlacaDoFeixe.Confere(campo, valor, Pedido));

    [Fact]
    public void Tratamento_e_ano_nao_estao_no_pedido_e_sem_pedido_nada_confere()
    {
        Assert.Null(PlacaDoFeixe.Confere(CampoOcr.Tratamento, "CCA", Pedido));
        Assert.Null(PlacaDoFeixe.Confere(CampoOcr.Ano, "2025", Pedido));
        Assert.Null(PlacaDoFeixe.Confere(CampoOcr.Fornecedor, "TG", null));
    }
}