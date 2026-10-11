namespace ViaPermanente.Dominio.Dtos.Apresentacao;

/// <summary>O pedido de exemplo criado para a apresentação e o fluxo dele.</summary>
public record ExemploCriadoDto(int IdPedido, string CodigoPedido, string Armazem, string Pilha, IReadOnlyList<EtapaFluxoDto> Etapas);