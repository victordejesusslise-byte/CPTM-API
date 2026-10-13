namespace ViaPermanente.Dominio.Catalogos.Models;

/// <summary>CODIGO_MATERIAL: o código do ERP depende do tamanho e da classe.</summary>
public class CodigoMaterial
{
    public int IdCodigo { get; set; }
    public int IdTamanho { get; set; }
    public byte Classe { get; set; }
    public string CodigoErp { get; set; } = "";
}