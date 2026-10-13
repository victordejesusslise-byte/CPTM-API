namespace ViaPermanente.Dominio.Via.Dtos;

/// <summary>Uma leitura da passagem: a peça (se a tag é conhecida), o km, se a tag respondeu, o desvio e a suspeita de rachadura.</summary>
public record LeituraDaPassagemDto(long Id, int? IdDormente, string? CodigoPeca, decimal? Km, bool Leitura, decimal? DesvioMm, bool RachaduraSuspeita, bool ForaDePosicao, DateTime Data, string? NotaPeca = null);

/// <summary>A passagem aberta: o resumo e as leituras (as com problema primeiro).</summary>
public record PassagemDetalheDto(PassagemDto Resumo, string? Veiculo, decimal? KmInicio, decimal? KmFim, decimal LimiteMm, IReadOnlyList<LeituraDaPassagemDto> Leituras);
