namespace ViaPermanente.Dominio.Dtos.Catalogos;

/// <summary>A pilha com quantas peças estão nela agora.</summary>
public record PilhaDto(int Id, string Codigo, string Setor, decimal ComprimentoM, byte Classe, int Pecas);