namespace ViaPermanente.Dominio.Pecas;

/// <summary>Onde estão as fotos de uma peça: a da verificação de qualidade e os quadros da câmera do carro.</summary>
public interface IFotoPecaRepository
{
    Task<bool> PecaExiste(int idDormente);
    Task<IReadOnlyList<(string Arquivo, DateTime Data, string Resultado)>> DaEntrada(int idDormente);
    Task<IReadOnlyList<(string Arquivo, DateTime Data, string Defeito, byte Grau, decimal Confianca)>> DaCamera(int idDormente);
}
