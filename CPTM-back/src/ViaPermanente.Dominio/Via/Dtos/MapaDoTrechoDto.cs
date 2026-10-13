namespace ViaPermanente.Dominio.Via.Dtos;

/// <summary>Um trecho da via na lista do mapa: as estações, a linha, os km e quantas peças estão em Ruim ou Crítico.</summary>
public record TrechoDoMapaDto(int Id, string Nome, string Linha, decimal KmInicio, decimal KmFim, int Pecas, int RuimOuCritico);

/// <summary>Uma peça assentada no trecho, com o ID de posição (L09-213100-0012) e a nota única.</summary>
public record PecaNoTrechoDto(int Id, string Codigo, string? Tag, string? Posicao, decimal Km, string? Nota);

/// <summary>Uma faixa do trecho (100 m por padrão) com a contagem de cada nota e as peças dela.</summary>
public record FaixaDoTrechoDto(decimal KmInicio, decimal KmFim, int Bom, int Regular, int Ruim, int Critico, int SemNota, IReadOnlyList<PecaNoTrechoDto> Pecas);

/// <summary>A tela do trecho: a distribuição das notas ao longo do km.</summary>
public record MapaDoTrechoDto(TrechoDoMapaDto Trecho, int FaixaM, IReadOnlyList<FaixaDoTrechoDto> Faixas);
