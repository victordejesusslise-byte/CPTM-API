namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>Os passos da troca.</summary>
public enum StatusTroca
{
    /// <summary>o técnico pediu; espera a aprovação.</summary>
    Pedida,
    /// <summary>aprovada e há peça compatível no armazém.</summary>
    Aprovada,
    /// <summary>aprovada, mas sem peça compatível no armazém.</summary>
    AguardandoPeca,
    /// <summary>o pátio separou a peça que vai entrar.</summary>
    Separada,
    /// <summary>a via trocou a peça.</summary>
    Feita,
    /// <summary>quem aprova reprovou.</summary>
    Reprovada,
    /// <summary>a via não conseguiu trocar; espera o técnico.</summary>
    Recusada,
    /// <summary>cancelada (recusa aceita, inspeção corrigida ou feita num lote).</summary>
    Cancelada
}
