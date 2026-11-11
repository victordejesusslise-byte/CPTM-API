namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>De onde veio o pedido de troca.</summary>
public enum OrigemTroca
{
    Inspecao,
    Requisicao,
    /// <summary>A troca feita num lote de troca (a peça que sai só aparece na noite).</summary>
    Lote
}
