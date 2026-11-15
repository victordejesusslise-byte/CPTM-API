namespace ViaPermanente.Dominio.Pecas;

/// <summary>Onde estão as fotos de uma peça: a da verificação de qualidade e os quadros da câmera do carro.</summary>
public interface IFotoPecaRepository
{
    /// <summary>A peça existe?</summary>
    Task<bool> PecaExiste(int idDormente);
    /// <summary>As fotos da verificação de qualidade, com a data e o resultado.</summary>
    Task<IReadOnlyList<(string Arquivo, DateTime Data, string Resultado)>> DaEntrada(int idDormente);
    /// <summary>Os quadros da câmera do carro, com o defeito, o grau e a confiança (de 0 a 1).</summary>
    Task<IReadOnlyList<(string Arquivo, DateTime Data, string Defeito, byte Grau, decimal Confianca)>> DaCamera(int idDormente);
}
