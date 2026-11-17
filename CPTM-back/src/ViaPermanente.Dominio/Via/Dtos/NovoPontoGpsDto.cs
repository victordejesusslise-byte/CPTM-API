namespace ViaPermanente.Dominio.Via.Dtos;

/// <summary>Um ponto do caminho do veículo: o GPS na hora em que a câmera tirou uma foto (o bip).</summary>
public class NovoPontoGpsDto
{
    /// <summary>A hora do GPS. Usa-se a do GPS em tudo, para não misturar com o relógio da câmera.</summary>
    public DateTime Data { get; init; }
    /// <summary>A latitude, em graus (−90 a 90).</summary>
    public decimal Latitude { get; init; }
    /// <summary>A longitude, em graus (−180 a 180).</summary>
    public decimal Longitude { get; init; }
}
