using System.Security.Cryptography;

namespace ViaPermanente.Dominio.Helpers;

/// <summary>
/// Hash de senha com PBKDF2 (SHA-256, 100 mil voltas, sal aleatório). A senha nunca é guardada.
/// Formato: pbkdf2-sha256$voltas$sal$hash (base64).
/// </summary>
public static class SenhaHelper
{
    private const int Voltas = 100_000;
    private const int TamanhoSal = 16;
    private const int TamanhoHash = 32;

    public static string GerarHash(string senha)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanhoSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Voltas, HashAlgorithmName.SHA256, TamanhoHash);
        return $"pbkdf2-sha256${Voltas}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>Confere a senha com o hash guardado. Hash vazio ou fora do formato devolve falso.</summary>
    public static bool Conferir(string senha, string? hashGuardado)
    {
        var partes = hashGuardado?.Split('$');
        if (partes is not { Length: 4 } || partes[0] != "pbkdf2-sha256" || !int.TryParse(partes[1], out var voltas))
            return false;

        try
        {
            var sal = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, sal, voltas, HashAlgorithmName.SHA256, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}