namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// O técnico pede um lote de troca (Victor, 2026-11-11): o trecho, a faixa de km, quantas peças e o comprimento delas. Não escolhe as peças:
/// na noite, a via lê a tag de cada dormente que sai.
/// </summary>
public class NovoLoteTrocaDto
{
    public int IdTrecho { get; init; }
    public decimal KmInicio { get; init; }
    public decimal KmFim { get; init; }
    public int Quantidade { get; init; }
    public int IdTamanho { get; init; }
    public string Motivo { get; init; } = "";
    public string? Observacao { get; init; }
}

/// <summary>Aprovar (o lote inteiro, podendo baixar a quantidade com o porquê em Motivo) ou reprovar (com motivo). Urgente: entra na noite de hoje mesmo depois do corte.</summary>
public class DecisaoLoteTrocaDto
{
    public bool Aprovar { get; init; }
    public int? Quantidade { get; init; }
    public string? Motivo { get; init; }
    public bool Urgente { get; init; }
}
