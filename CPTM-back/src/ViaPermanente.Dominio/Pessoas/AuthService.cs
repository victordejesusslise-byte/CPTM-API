using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Pessoas.Dtos;

namespace ViaPermanente.Dominio.Pessoas;

/// <summary>Confere e-mail e senha. Não diz qual dos dois errou.</summary>
public class AuthService(IUsuarioRepository usuarios)
{
    /// <summary>Devolve o usuário se o e-mail existe, o usuário está ativo e a senha confere; senão, nulo.</summary>
    public async Task<UsuarioDto?> Entrar(LoginDto login)
    {
        var encontrado = await usuarios.BuscarPorEmail(EmailHelper.Normalizar(login.Email));
        if (encontrado is null || !encontrado.Usuario.Ativo)
            return null;

        return SenhaHelper.Conferir(login.Senha, encontrado.Usuario.SenhaHash)
            ? UsuarioService.ParaDto(encontrado)
            : null;
    }
}