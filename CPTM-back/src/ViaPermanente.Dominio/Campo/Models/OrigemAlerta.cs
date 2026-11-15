namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>Sensor (o carro mede) ou imagem (a câmera vê).</summary>
public enum OrigemAlerta
{
    /// <summary>o carro mediu: tag muda, rachadura ou posição.</summary>
    Sensor,
    /// <summary>a câmera do carro viu: rachadura, fenda ou quebra.</summary>
    Imagem
}
