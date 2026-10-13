
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>SENSOR: fica num leitor (pistola ou carro).</summary>
public class Sensor
{
    public int IdSensor { get; set; }
    public int IdLeitor { get; set; }
    public string TipoSensor { get; set; } = "";
    public string? IdSensorRw { get; set; }
    public bool Ativo { get; set; } = true;
}