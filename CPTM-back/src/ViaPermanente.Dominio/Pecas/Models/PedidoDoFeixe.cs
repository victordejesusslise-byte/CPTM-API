namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>O pedido de onde o feixe veio, para montar a tag do fornecedor: o código, a sigla do fornecedor e as peças já cadastradas do pedido.</summary>
/// <param name="IdPedido">Id do pedido (→ PEDIDO).</param>
/// <param name="Codigo">O código do pedido, ex.: PD-20261017-195108.</param>
/// <param name="Sigla">A sigla da empresa fornecedora, ex.: TG. Nula se não cadastrada.</param>
/// <param name="Cadastradas">Quantas peças do pedido já foram cadastradas.</param>
public record PedidoDoFeixe(int IdPedido, string Codigo, string? Sigla, int Cadastradas);
