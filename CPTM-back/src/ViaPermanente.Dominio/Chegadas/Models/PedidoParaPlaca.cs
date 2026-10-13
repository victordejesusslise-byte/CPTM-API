namespace ViaPermanente.Dominio.Chegadas.Models;

/// <summary>O que o pedido diz e a placa pode conferir: fornecedor, ata, contrato, espécies (com a classe) e comprimentos.</summary>
public record PedidoParaPlaca(string Fornecedor, string? Ata, string? Contrato, IReadOnlyList<(string Especie, byte Classe)> Especies,
    IReadOnlyList<byte> Classes, IReadOnlyList<decimal> Comprimentos);