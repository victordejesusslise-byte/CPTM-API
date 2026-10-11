namespace ViaPermanente.Dominio.Dtos.Ocr;

/// <summary>
/// Um campo lido. ConferePedido nulo: "sem pedido". Pronto: confiança acima do mínimo e confere com o pedido;
/// senão, vem aberto para o operador conferir com a foto. O valor do pedido não aparece.
/// </summary>
public record CampoOcrDto(int Id, string Campo, string? ValorLido, decimal? Confianca, bool? ConferePedido, bool Pronto,
    string? ValorConfirmado, bool? Corrigido, int? CaixaX, int? CaixaY, int? CaixaLargura, int? CaixaAltura);

public record LeituraOcrDto(int Id, string FotoArquivo, string? TextoLido, decimal? ConfiancaMedia, int VersaoModelo,
    bool Confirmada, IReadOnlyList<CampoOcrDto> Campos);

public record ModeloOcrDto(int Id, string Nome, int Versao, string ModeloBase, DateTime? TreinadoEm, int Amostras, decimal? AcertoTeste, bool Ativo);

public class CampoConfirmadoDto
{
    public string Campo { get; init; } = "";
    public string Valor { get; init; } = "";
}

/// <summary>Os valores que o operador confirmou. Campo não enviado fica confirmado como foi lido.</summary>
public class ConfirmarPlacaDto
{
    public List<CampoConfirmadoDto> Campos { get; init; } = [];
    public int? IdRecebimento { get; init; }
    public int? IdFeixe { get; init; }
}