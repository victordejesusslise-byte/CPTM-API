namespace ViaPermanente.Dominio.Via.Dtos;

/// <summary>
/// Uma leitura da passagem: a peça (se a tag é conhecida), o km, se a tag respondeu, o desvio e a suspeita de rachadura.
/// Latitude e Longitude: o GPS do veículo na hora da leitura (vazios nas passagens sem GPS).
/// </summary>
public record LeituraDaPassagemDto(long Id, int? IdDormente, string? CodigoPeca, decimal? Km, bool Leitura, decimal? DesvioMm, bool RachaduraSuspeita, bool ForaDePosicao, DateTime Data, string? NotaPeca = null,
    decimal? Latitude = null, decimal? Longitude = null);

/// <summary>
/// A passagem aberta: o resumo e as leituras (as com problema primeiro). EstacaoInicio e EstacaoFim: as estações da passagem (o bip 0 e o fim);
/// PontosGps: quantos pontos o caminho do GPS tem. Vazios nas passagens sem GPS.
/// </summary>
public record PassagemDetalheDto(PassagemDto Resumo, string? Veiculo, decimal? KmInicio, decimal? KmFim, decimal LimiteMm, IReadOnlyList<LeituraDaPassagemDto> Leituras,
    string? EstacaoInicio = null, string? EstacaoFim = null, int PontosGps = 0);
