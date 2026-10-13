namespace ViaPermanente.Dominio.Chegadas.Dtos;

/// <summary>O feixe e quantas peças dele já foram cadastradas.</summary>
public record FeixeDto(int Id, string Numero, int Quantidade, int Cadastradas, int IdEspecie, string Especie, byte Classe,
    int IdTamanho, decimal ComprimentoM, string? Tratamento, int? IdEmpresaTratamento, int? IdEmpresaInspecao, short? AnoInspecao);