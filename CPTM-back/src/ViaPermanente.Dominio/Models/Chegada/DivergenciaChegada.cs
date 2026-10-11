namespace ViaPermanente.Dominio.Models.Chegada;

/// <summary>A quantidade da chegada não bate: o que a nota diz, o que o operador informou e o que os feixes somam.</summary>
public record DivergenciaChegada(int? QuantidadeNota, int QuantidadeInformada, int SomaFeixes, string Mensagem);