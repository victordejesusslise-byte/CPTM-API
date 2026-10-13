
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>TIPO_LEITOR: pistola, módulo do celular ou carro.</summary>
public class TipoLeitor
{
    public int IdTipoLeitor { get; set; }
    public string Nome { get; set; } = "";
    public string? Descricao { get; set; }
}