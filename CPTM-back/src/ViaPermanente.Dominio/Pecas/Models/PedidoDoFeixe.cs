namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>O pedido de onde o feixe veio, para montar a tag do fornecedor: o código, a sigla do fornecedor e as peças já cadastradas do pedido.</summary>
public record PedidoDoFeixe(int IdPedido, string Codigo, string? Sigla, int Cadastradas);
