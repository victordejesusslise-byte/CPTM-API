using ViaPermanente.Dominio.Dtos.Ofertas;

namespace ViaPermanente.Dominio.Dtos.Pedidos;

/// <summary>O aviso da tela do pedido: a 2ª classe (bloqueia acima do limite) e a oferta (só avisa).</summary>
public record VerificacaoPedidoDto(LimiteDto SegundaClasse, IReadOnlyList<AvisoOfertaDto> Oferta);