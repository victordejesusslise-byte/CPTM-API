using ViaPermanente.Dominio.Comum;

namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>LEITURA_DORMENTE: cada leitura da tag de uma peça, mesmo a que deu errado (ambígua ou sem resposta).
/// A leitura só fica ligada à peça quando havia uma tag só no alcance (decisão: Nada muda de estado sem alvo único).</summary>
public class LeituraDormente : IRegistroDeCampo
{
    /// <summary>Id da leitura.</summary>
    public int IdLeitura { get; set; }
    /// <summary>Id da peça lida (→ DORMENTE). Nulo: tag sem cadastro ou leitura ambígua.</summary>
    public int? IdDormente { get; set; }
    /// <summary>Id do leitor que leu (→ LEITOR). Nulo se foi digitado.</summary>
    public int? IdLeitor { get; set; }
    /// <summary>Id de quem leu (→ USUARIO).</summary>
    public int IdUsuario { get; set; }
    /// <summary>Como a peça foi identificada: módulo, câmera, NFC ou digitado.</summary>
    public MetodoCaptura MetodoCaptura { get; set; }
    /// <summary>A tag respondeu? Falso: a tag esperada não respondeu.</summary>
    public bool Leitura { get; set; }
    /// <summary>Quando a tag foi lida.</summary>
    public DateTime DataLeitura { get; set; }
    /// <summary>Umidade medida por um sensor do leitor, se houver (hoje nada preenche).</summary>
    public decimal? Umidade { get; set; }
    /// <summary>Temperatura medida por um sensor do leitor, se houver (hoje nada preenche).</summary>
    public decimal? Temperatura { get; set; }
    /// <summary>Texto livre, ex.: por que a tag não respondeu.</summary>
    public string? Observacao { get; set; }
    /// <summary>O número lido (BR e 10 dígitos).</summary>
    public string? TagLida { get; set; }
    /// <summary>Id do modo de leitura: Armazém, Normal, Via ou Personalizado (→ MODO_LEITURA).</summary>
    public int? IdModo { get; set; }
    /// <summary>A potência usada pelo leitor, em dBm.</summary>
    public decimal? PotenciaDbm { get; set; }
    /// <summary>A força do sinal da tag, em dBm.</summary>
    public decimal? RssiDbm { get; set; }
    /// <summary>Quantas tags o leitor viu no alcance.</summary>
    public int? TagsVistas { get; set; }
    /// <summary>Verdadeiro: havia mais de uma tag no alcance, então a leitura não vale para nenhuma peça.</summary>
    public bool Ambigua { get; set; }
    /// <summary>Id da conferência do armazém (→ CONFERENCIA_ARMAZEM). Nulo fora da conferência.</summary>
    public int? IdConferencia { get; set; }
    /// <summary>Id da pilha onde a peça respondeu na conferência (→ PILHA). Nulo fora dela.</summary>
    public int? IdPilha { get; set; }
    /// <summary>Onde a tag sem cadastro foi lida, em km com 3 casas, se o operador informou.</summary>
    public decimal? Km { get; set; }
    /// <summary>Verdadeiro: o operador da via avisou uma tag sem cadastro, para o técnico resolver.</summary>
    public bool Aviso { get; set; }
    /// <summary>Gerado no celular: o mesmo envio repetido entra uma vez.</summary>
    public Guid? IdCliente { get; set; }
    /// <summary>A hora do celular; sem ela, a do servidor.</summary>
    public DateTime RegistradoEm { get; set; }
    /// <summary>A hora em que chegou ao servidor.</summary>
    public DateTime? RecebidoEm { get; set; }
}