namespace ViaPermanente.Dominio.Pedidos.Dtos;

public record NotaItemDto(int Id, int IdEspecie, string Especie, int IdTamanho, decimal ComprimentoM, byte Classe,
    int Quantidade, decimal VolumeM3Declarado, decimal VolumeM3Catalogo);