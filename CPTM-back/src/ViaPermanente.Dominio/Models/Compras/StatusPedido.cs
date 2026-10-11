namespace ViaPermanente.Dominio.Models.Compras;

/// <summary>Recebido e parcial não são status: saem de RECEBIMENTO.</summary>
public enum StatusPedido
{
    Rascunho,
    Aberto,
    EmTransito,
    Cancelado
}
