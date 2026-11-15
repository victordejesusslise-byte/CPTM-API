
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>SENSOR: um sensor que fica num leitor (pistola ou carro).</summary>
public class Sensor
{
    /// <summary>Id do sensor.</summary>
    public int IdSensor { get; set; }
    /// <summary>Id do leitor que carrega o sensor (→ LEITOR).</summary>
    public int IdLeitor { get; set; }
    /// <summary>O tipo do sensor, em texto.</summary>
    public string TipoSensor { get; set; } = "";
    /// <summary>O id do sensor no próprio aparelho, se houver.</summary>
    public string? IdSensorRw { get; set; }
    /// <summary>Falso: o sensor saiu de uso.</summary>
    public bool Ativo { get; set; } = true;
}