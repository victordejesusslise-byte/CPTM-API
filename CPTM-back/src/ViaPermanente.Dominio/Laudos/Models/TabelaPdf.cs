namespace ViaPermanente.Dominio.Laudos.Models;

public record TabelaPdf(IReadOnlyList<string> Colunas, IReadOnlyList<IReadOnlyList<string>> Linhas);