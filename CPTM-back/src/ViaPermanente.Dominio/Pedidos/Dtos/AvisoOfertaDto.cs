namespace ViaPermanente.Dominio.Pedidos.Dtos;

/// <summary>Aviso ao montar o pedido: o fornecedor não oferta o item, ou o m³ pedido passa do ofertado. Não bloqueia.</summary>
public record AvisoOfertaDto(int IdTamanho, byte Classe, int? IdEspecie, decimal VolumeM3Pedido, decimal? VolumeM3Ofertado, string Mensagem);