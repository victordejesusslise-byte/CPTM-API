namespace ViaPermanente.Dominio.Catalogos.Models;

/// <summary>TAMANHO_DORMENTE: o m³ é calculado (comprimento × largura × altura), não guardado.</summary>
public class TamanhoDormente
{
    public int IdTamanho { get; set; }
    public decimal ComprimentoM { get; set; }
    public decimal LarguraM { get; set; }
    public decimal AlturaM { get; set; }
    public bool MedidaProvisoria { get; set; }
    /// <summary>O código de material do ERP para este tamanho na 1ª e na 2ª classe (era a tabela CODIGO_MATERIAL, até 2026-10-23).</summary>
    public string? CodigoErpClasse1 { get; set; }
    public string? CodigoErpClasse2 { get; set; }

    /// <summary>O código do ERP da classe (1 ou 2); nulo se ainda não cadastrado.</summary>
    public string? CodigoErp(byte classe) => classe == 1 ? CodigoErpClasse1 : classe == 2 ? CodigoErpClasse2 : null;
}