using ViaPermanente.Dominio.Chegadas;

namespace ViaPermanente.Testes.Helpers;

/// <summary>
/// O texto que o OCR leu é guardado em LEITURA_OCR.texto_lido, que tem 1000 caracteres. A foto inteira da câmera do celular pode
/// render mais que isso (2026-11-15: o app dava erro 500 ao ler a placa); o texto guardado é cortado para caber.
/// </summary>
public class TextoDoOcrTestes
{
    [Fact]
    public void Texto_longo_e_cortado_para_caber_na_coluna()
    {
        var longo = new string('x', 1500);
        var guardado = OcrPlacaService.TextoParaGuardar(longo);
        Assert.Equal(OcrPlacaService.TamanhoDoTextoLido, guardado!.Length);
    }

    /// <summary>
    /// O valor de um campo vai para OCR_CAMPO.valor_lido (e valor_confirmado), de 200 caracteres. Na placa fotografada na tela, a leitura
    /// de 800 px juntou lixo das listras num campo só e o banco recusou (erro 500 no app, 2026-11-15).
    /// </summary>
    [Fact]
    public void Valor_do_campo_longo_e_cortado_para_caber_na_coluna()
    {
        var guardado = OcrPlacaService.ValorParaGuardar(new string('x', 300));
        Assert.Equal(OcrPlacaService.TamanhoDoValor, guardado.Length);
        Assert.Equal("AR00224", OcrPlacaService.ValorParaGuardar(" AR00224 "));
    }

    [Fact]
    public void Texto_curto_fica_igual_sem_os_espacos_das_pontas()
    {
        Assert.Equal("ATA 123", OcrPlacaService.TextoParaGuardar("  ATA 123 \n"));
        Assert.Null(OcrPlacaService.TextoParaGuardar("   "));
    }
}
