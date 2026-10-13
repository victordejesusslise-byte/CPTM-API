
namespace ViaPermanente.Dominio.Pedidos.Dtos;

/// <summary>O aviso da tela do pedido: a 2ª classe (bloqueia acima do limite) e a oferta (só avisa).</summary>
public record VerificacaoPedidoDto(LimiteDto SegundaClasse, IReadOnlyList<AvisoOfertaDto> Oferta);