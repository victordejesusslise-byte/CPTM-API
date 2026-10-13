namespace ViaPermanente.Dominio.Integracoes.Dtos;

public record NotaImportadaDto(string Pedido, string Numero, int IdNota, string StatusConferencia, IReadOnlyList<string> Divergencias);

/// <summary>O resultado da importação: as notas que entraram (já conferidas) e os erros, que também ficam em INTEGRACAO_LOG.</summary>
public record ImportacaoNotasDto(string Arquivo, IReadOnlyList<NotaImportadaDto> Notas, IReadOnlyList<string> Erros);

public record IntegracaoLogDto(int Id, DateTime Data, string Sistema, string Direcao, string Tipo, string Status, string? Mensagem, string? Arquivo);