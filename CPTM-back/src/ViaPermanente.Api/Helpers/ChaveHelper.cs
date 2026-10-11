using System.Security.Cryptography;
using System.Text;

namespace ViaPermanente.Api.Helpers;

/// <summary>Compara a chave recebida com a configurada em tempo constante (não vaza, pelo tempo, quantos caracteres batem).</summary>
public static class ChaveHelper
{
    public static bool Confere(string? recebida, string? configurada)
        => !string.IsNullOrEmpty(recebida) && !string.IsNullOrEmpty(configurada)
           && CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(recebida)), SHA256.HashData(Encoding.UTF8.GetBytes(configurada)));
}