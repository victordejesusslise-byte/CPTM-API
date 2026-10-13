namespace ViaPermanente.Dominio.Ambiental.Models;

/// <summary>Uma linha da planilha de notas (modelo provisório), como veio. Erro: o que não deu para ler na linha.</summary>
public record LinhaNotaPlanilha(int Linha, string? Pedido, string? Nota, DateOnly? Emissao, decimal? Valor, string? Especie,
    decimal? ComprimentoM, int? Classe, int? Quantidade, decimal? VolumeM3, string? Erro);