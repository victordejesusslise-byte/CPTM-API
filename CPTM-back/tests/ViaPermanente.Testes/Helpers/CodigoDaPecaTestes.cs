using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Testes.Helpers;

/// <summary>O código da peça (DM-E01-L01-0028), gravado em DORMENTE.codigo no cadastro: empresa e lote com 2 dígitos, a peça com 4.</summary>
public class CodigoDaPecaTestes
{
    [Theory]
    [InlineData(1, 1, 28, "DM-E01-L01-0028")]
    [InlineData(1, 1002, 1030, "DM-E01-L1002-1030")]
    [InlineData(12, 3, 12345, "DM-E12-L03-12345")]
    public void Monta_o_codigo_pela_empresa_pelo_lote_e_pela_peca(int idEmpresa, int idLote, int idDormente, string esperado)
        => Assert.Equal(esperado, Dormente.MontarCodigo(idEmpresa, idLote, idDormente));
}
