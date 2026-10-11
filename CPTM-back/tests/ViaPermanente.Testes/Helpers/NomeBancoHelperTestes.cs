using ViaPermanente.Dados.Helpers;

namespace ViaPermanente.Testes.Helpers;

public class NomeBancoHelperTestes
{
    [Theory]
    [InlineData("ClassificacaoMadeira", "CLASSIFICACAO_MADEIRA")]
    [InlineData("Rfid", "RFID")]
    [InlineData("LeituraOcr", "LEITURA_OCR")]
    [InlineData("RelatorioAmbientalItem", "RELATORIO_AMBIENTAL_ITEM")]
    public void Tabela_em_maiusculas_com_sublinhado(string classe, string tabela)
        => Assert.Equal(tabela, NomeBancoHelper.Tabela(classe));

    [Theory]
    [InlineData("IdDormente", "id_dormente")]
    [InlineData("VolumeM3Estoque", "volume_m3_estoque")]
    [InlineData("AMais", "a_mais")]
    [InlineData("DuracaoS", "duracao_s")]
    [InlineData("FolgaRssiDb", "folga_rssi_db")]
    [InlineData("CaixaX", "caixa_x")]
    public void Coluna_em_minusculas_com_sublinhado(string propriedade, string coluna)
        => Assert.Equal(coluna, NomeBancoHelper.Coluna(propriedade));

    [Theory]
    [InlineData("comprimento_m", 5, 3)]
    [InlineData("km_medido", 9, 3)]
    [InlineData("volume_m3", 12, 5)]
    [InlineData("desvio_mm", 8, 2)]
    [InlineData("rssi_dbm", 5, 1)]
    [InlineData("confianca", 7, 4)]
    public void Precisao_pela_unidade_da_coluna(string coluna, int precisao, int escala)
        => Assert.Equal((precisao, escala), NomeBancoHelper.PrecisaoDecimal(coluna));
}