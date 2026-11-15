namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>Os passos do lote de troca: pedido, aprovado (ou aguardando peça), separado e feito; ou reprovado, recusado pela via, cancelado.</summary>
public enum StatusLoteTroca
{
    /// <summary>o técnico pediu; espera a aprovação.</summary>
    Pedido,
    /// <summary>aprovado e há peça compatível no armazém.</summary>
    Aprovado,
    /// <summary>aprovado, mas sem peça compatível no armazém.</summary>
    AguardandoPeca,
    /// <summary>o pátio já separou peças para o lote.</summary>
    Separado,
    /// <summary>a via fechou o lote.</summary>
    Feito,
    /// <summary>quem aprova reprovou.</summary>
    Reprovado,
    /// <summary>a via não conseguiu trocar; espera o técnico.</summary>
    Recusado,
    /// <summary>o técnico aceitou a recusa.</summary>
    Cancelado
}
