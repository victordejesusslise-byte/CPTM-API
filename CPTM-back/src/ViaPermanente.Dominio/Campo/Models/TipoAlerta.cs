namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>Sensor: rachadura, posição, tag muda. Imagem: rachadura, fenda, quebra.</summary>
public enum TipoAlerta
{
    /// <summary>rachadura (sensor ou câmera).</summary>
    Rachadura,
    /// <summary>a peça saiu do lugar além do limite (sensor).</summary>
    Posicao,
    /// <summary>a tag não respondeu ao carro (sensor).</summary>
    TagMuda,
    /// <summary>fenda vista pela câmera.</summary>
    Fenda,
    /// <summary>quebra vista pela câmera.</summary>
    Quebra
}
