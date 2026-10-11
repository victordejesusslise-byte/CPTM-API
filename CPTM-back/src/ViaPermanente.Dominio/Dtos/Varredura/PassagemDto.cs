namespace ViaPermanente.Dominio.Dtos.Varredura;

/// <summary>O resumo da noite: o que foi lido e o que virou alerta.</summary>
public record PassagemDto(int Id, string Codigo, DateTime DataInicio, DateTime? DataFim,
    int Leituras, int SemResposta, int ForaDePosicao, int TagsDesconhecidas, int MudancasDeFaixa,
    int AlertasSensor, int AlertasImagem, int DeteccoesAbaixoDoMinimo);