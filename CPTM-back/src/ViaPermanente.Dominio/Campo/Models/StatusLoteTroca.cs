namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>Os passos do lote de troca: pedido, aprovado (ou aguardando peça), separado e feito; ou reprovado, recusado pela via, cancelado.</summary>
public enum StatusLoteTroca
{
    Pedido,
    Aprovado,
    AguardandoPeca,
    Separado,
    Feito,
    Reprovado,
    Recusado,
    Cancelado
}
