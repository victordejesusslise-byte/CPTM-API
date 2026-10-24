namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>
/// Os tipos de leitor, fixos no código (Victor, 2026-10-23: saiu a tabela TIPO_LEITOR). O número é o que vai no banco (LEITOR.tipo)
/// e na API (idTipoLeitor). Cada tipo tem comportamento próprio no sistema: o módulo grava e lê as tags, a pistola mede a madeira,
/// o carro manda a passagem da noite.
/// </summary>
public enum TipoLeitor
{
    ModuloRfid = 1,
    PistolaNir = 2,
    Carro = 3
}

/// <summary>O nome de cada tipo de leitor, para a tela e para a API.</summary>
public static class TiposLeitor
{
    public static string Nome(TipoLeitor tipo) => tipo switch
    {
        TipoLeitor.ModuloRfid => "Módulo RFID (celular)",
        TipoLeitor.PistolaNir => "Pistola NIR",
        TipoLeitor.Carro => "Carro de inspeção",
        _ => tipo.ToString()
    };
}
