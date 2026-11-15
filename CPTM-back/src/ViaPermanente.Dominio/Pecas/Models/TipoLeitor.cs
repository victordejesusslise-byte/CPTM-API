namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>Os tipos de leitor, fixos no código (Victor, 2026-10-23: saiu a tabela TIPO_LEITOR). O número vai no banco
/// (LEITOR.tipo) e na API. O módulo lê e grava as tags, a pistola mede a madeira e o carro manda a passagem da noite.</summary>
public enum TipoLeitor
{
    /// <summary>O módulo RFID ligado ao celular: lê e grava as tags.</summary>
    ModuloRfid = 1,
    /// <summary>A pistola NIR: analisa a madeira (espécie e umidade).</summary>
    PistolaNir = 2,
    /// <summary>O carro de inspeção que passa na via à noite.</summary>
    Carro = 3
}

/// <summary>O nome de cada tipo de leitor, para a tela e para a API.</summary>
public static class TiposLeitor
{
    /// <summary>O nome do tipo para mostrar; tipo desconhecido mostra o próprio valor.</summary>
    public static string Nome(TipoLeitor tipo) => tipo switch
    {
        TipoLeitor.ModuloRfid => "Módulo RFID (celular)",
        TipoLeitor.PistolaNir => "Pistola NIR",
        TipoLeitor.Carro => "Carro de inspeção",
        _ => tipo.ToString()
    };
}
