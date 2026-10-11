using System.Net.Mail;

namespace ViaPermanente.Dominio.Helpers;

/// <summary>O e-mail é o login: sempre guardado e comparado sem espaços e em minúsculas.</summary>
public static class EmailHelper
{
    public static string Normalizar(string? email) => (email ?? "").Trim().ToLowerInvariant();

    public static bool PareceValido(string? email)
    {
        var normalizado = Normalizar(email);
        return normalizado.Length > 0 && MailAddress.TryCreate(normalizado, out var endereco) && endereco.Address == normalizado;
    }
}