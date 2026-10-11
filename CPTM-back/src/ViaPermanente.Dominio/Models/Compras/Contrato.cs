namespace ViaPermanente.Dominio.Models.Compras;

/// <summary>CONTRATO: a compra é por ata de registro; o contrato sai da ata.</summary>
public class Contrato
{
    public int IdContrato { get; set; }
    public int IdEmpresa { get; set; }
    public string NumeroAta { get; set; } = "";
    public string NumeroContrato { get; set; } = "";
    public DateOnly? VigenciaInicio { get; set; }
    public DateOnly? VigenciaFim { get; set; }
}