namespace ViaPermanente.Dominio.Dtos.Indicadores;

public record DetalheIndicadorDto(string Rotulo, string Valor);

/// <summary>Um indicador do painel: valor (nulo quando ainda não há dado), unidade, resumo, referência e se está no vermelho.</summary>
public record IndicadorDto(string Chave, string Nome, decimal? Valor, string Unidade, string Resumo, string? Referencia, bool Vermelho,
    IReadOnlyList<DetalheIndicadorDto> Detalhes);