namespace ViaPermanente.Dominio.Pedidos.Dtos;

public record PedidoItemDto(int Id, int IdTamanho, decimal ComprimentoM, byte Classe, int? IdEspecie, string? Especie, int Quantidade, decimal VolumeM3, int IdMedida, string Medida);