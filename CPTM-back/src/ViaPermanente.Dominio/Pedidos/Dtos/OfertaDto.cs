namespace ViaPermanente.Dominio.Pedidos.Dtos;

/// <summary>A oferta que vale hoje: a mais recente do fornecedor para a espécie e o tamanho.</summary>
public record OfertaDto(int Id, int IdEmpresa, string Empresa, int IdEspecie, string Especie,
    int IdTamanho, decimal ComprimentoM, decimal VolumeM3Disponivel, DateOnly Data);