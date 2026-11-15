
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>LEITOR: o cadastro de todos os leitores (módulo RFID do celular, pistola NIR e carro), com o tipo de cada um
/// (decisão: Leitor único, com tipo).</summary>
public class Leitor
{
    /// <summary>Id do leitor.</summary>
    public int IdLeitor { get; set; }
    /// <summary>O tipo, pelo número fixo no código: 1 Módulo RFID, 2 Pistola NIR, 3 Carro.</summary>
    public TipoLeitor Tipo { get; set; }
    /// <summary>Id do setor onde o leitor fica (→ SETOR).</summary>
    public int? IdSetor { get; set; }
    /// <summary>O código do aparelho, ex.: PST-01, MOD-03.</summary>
    public string Codigo { get; set; } = "";
    /// <summary>O número de série; é por ele que o celular reconhece o módulo.</summary>
    public string? NumeroSerie { get; set; }
    /// <summary>A versão do hardware.</summary>
    public string? VersaoHardware { get; set; }
    /// <summary>A versão do firmware, atualizada a cada conexão.</summary>
    public string? VersaoFirmware { get; set; }
    /// <summary>Falso: o leitor saiu de uso.</summary>
    public bool Ativo { get; set; } = true;
}