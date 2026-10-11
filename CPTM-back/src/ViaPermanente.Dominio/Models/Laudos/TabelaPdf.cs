namespace ViaPermanente.Dominio.Models.Laudos;

public record TabelaPdf(IReadOnlyList<string> Colunas, IReadOnlyList<IReadOnlyList<string>> Linhas);