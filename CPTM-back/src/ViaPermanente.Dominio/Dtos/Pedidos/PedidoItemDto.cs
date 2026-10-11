namespace ViaPermanente.Dominio.Dtos.Pedidos;

public record PedidoItemDto(int Id, int IdTamanho, decimal ComprimentoM, byte Classe, int? IdEspecie, string? Especie, int Quantidade, decimal VolumeM3);