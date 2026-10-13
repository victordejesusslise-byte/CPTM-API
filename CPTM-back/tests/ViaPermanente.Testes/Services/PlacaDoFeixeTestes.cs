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