namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>De onde veio o pedido de troca.</summary>
public enum OrigemTroca
{
    /// <summary>pedida a partir de uma inspeção reprovada.</summary>
    Inspecao,
    /// <summary>pedida pelo técnico com um motivo, sem inspeção.</summary>
    Requisicao,
    /// <summary>A troca feita num lote de troca (a peça que sai só aparece na noite).</summary>
    Lote
}
