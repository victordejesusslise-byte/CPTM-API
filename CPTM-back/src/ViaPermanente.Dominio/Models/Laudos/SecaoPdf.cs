namespace ViaPermanente.Dominio.Models.Laudos;

/// <summary>Uma seção do documento: título, campos e, se houver, uma tabela.</summary>
public record SecaoPdf(string Titulo, IReadOnlyList<CampoPdf> Campos, TabelaPdf? Tabela = null);