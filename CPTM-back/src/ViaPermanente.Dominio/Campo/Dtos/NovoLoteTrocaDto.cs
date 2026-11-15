namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// O técnico pede um lote de troca (Victor, 2026-11-11): o trecho, a faixa de km, quantas peças e o comprimento delas. Não escolhe as peças:
/// na noite, a via lê a tag de cada dormente que sai.
/// </summary>
public class NovoLoteTrocaDto
{
    /// <summary>o trecho do lote (→ TRECHO); dá a classe exigida.</summary>
    public int IdTrecho { get; init; }
    /// <summary>o começo da faixa, em km com 3 casas; não pode passar do KmFim.</summary>
    public decimal KmInicio { get; init; }
    /// <summary>o fim da faixa, em km com 3 casas.</summary>
    public decimal KmFim { get; init; }
    /// <summary>quantas peças trocar; pelo menos 1.</summary>
    public int Quantidade { get; init; }
    /// <summary>o comprimento das peças (→ TAMANHO_DORMENTE).</summary>
    public int IdTamanho { get; init; }
    /// <summary>por que pedir o lote; obrigatório.</summary>
    public string Motivo { get; init; } = "";
    /// <summary>a observação do pedido.</summary>
    public string? Observacao { get; init; }
}

/// <summary>Aprovar (o lote inteiro, podendo baixar a quantidade com o porquê em Motivo) ou reprovar (com motivo). Urgente: entra na noite de hoje mesmo depois do corte.</summary>
public class DecisaoLoteTrocaDto
{
    /// <summary>verdadeiro aprova o lote; falso reprova.</summary>
    public bool Aprovar { get; init; }
    /// <summary>quantas peças aprovar, de 1 até a pedida; nula: todas as pedidas.</summary>
    public int? Quantidade { get; init; }
    /// <summary>por que reprovou, ou por que aprovou menos que o pedido; obrigatório nesses dois casos.</summary>
    public string? Motivo { get; init; }
    /// <summary>depois do corte (20:00), faz o lote entrar no plano desta noite.</summary>
    public bool Urgente { get; init; }
}
