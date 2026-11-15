using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>A leitura da peça que abre a operação (verificar, guardar). Com o módulo, uma tag só no alcance.</summary>
public class LeituraDto
{
    /// <summary>O número lido da tag (BR e 10 dígitos).</summary>
    public string Tag { get; init; } = "";
    /// <summary>Como foi lida: módulo (o normal), câmera, NFC ou digitado.</summary>
    public MetodoCaptura Metodo { get; init; } = MetodoCaptura.ModuloUhf;
    /// <summary>Quantas tags o módulo viu no alcance; com o módulo, tem de ser 1.</summary>
    public int TagsVistas { get; init; } = 1;
    /// <summary>Id do leitor usado (→ LEITOR).</summary>
    public int? IdLeitor { get; init; }
    /// <summary>Id do modo de leitura (→ MODO_LEITURA).</summary>
    public int? IdModo { get; init; }
    /// <summary>A potência usada pelo leitor, em dBm.</summary>
    public decimal? PotenciaDbm { get; init; }
    /// <summary>A força do sinal da tag, em dBm.</summary>
    public decimal? RssiDbm { get; init; }
}