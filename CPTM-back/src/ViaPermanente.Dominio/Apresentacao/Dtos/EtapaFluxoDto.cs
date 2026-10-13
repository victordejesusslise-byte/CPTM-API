namespace ViaPermanente.Dominio.Apresentacao.Dtos;

/// <summary>Uma etapa do caminho da peça: quem faz, onde grava, quantas linhas tem e o resumo em português.</summary>
public record EtapaFluxoDto(int Etapa, string Nome, string QuemFaz, IReadOnlyList<string> Tabelas, int Linhas, bool Feita, string Resumo);