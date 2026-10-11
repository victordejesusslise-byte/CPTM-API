namespace ViaPermanente.Dominio.Models.Chegada;

/// <summary>Um campo da placa achado no texto lido.</summary>
public record CampoLido(CampoOcr Campo, string Valor, decimal Confianca, LinhaOcr Linha);