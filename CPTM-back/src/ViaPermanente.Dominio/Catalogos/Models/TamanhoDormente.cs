namespace ViaPermanente.Dominio.Catalogos.Models;

/// <summary>TAMANHO_DORMENTE: o m³ é calculado (comprimento × largura × altura), não guardado.</summary>
public class TamanhoDormente
{
    public int IdTamanho { get; set; }
    public decimal ComprimentoM { get; set; }
    public decimal LarguraM { get; set; }
    public decimal AlturaM { get; set; }
    public bool MedidaProvisoria { get; set; }
    public bool Ativo { get; set; } = true;
}