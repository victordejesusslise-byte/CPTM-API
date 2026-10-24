using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Pessoas.Dtos;

namespace ViaPermanente.Dominio.Pessoas;

/// <summary>O resultado do login: o usuário (nulo se não entrou), a versão da sessão que vai no token e, se a conta está esperando, até quando.</summary>
public record ResultadoLogin(UsuarioDto? Usuario, int VersaoSessao, DateTime? EsperarAte);

/// <summary>
/// Confere e-mail e senha (sem dizer qual dos dois errou) e a sessão de cada chamada.
/// Senhas erradas (Victor, 2026-10-18, "espera crescente"): depois de 5 seguidas na mesma conta, ela espera 30 s; na 6ª, 1 min; da 7ª
/// em diante, 5 min. Enquanto espera, nem a senha certa entra. O login certo zera a contagem. (O limite por aparelho fica na API.)
/// </summary>
public class AuthService(IUsuarioRepository usuarios)
{
    public const int ErrosLivres = 5;

    public async Task<ResultadoLogin> Entrar(LoginDto login, DateTime? agora = null)
    {
        var instante = agora ?? DateTime.Now;
        var encontrado = await usuarios.BuscarPorEmail(EmailHelper.Normalizar(login.Email));
        if (encontrado is null || !encontrado.Ativo)
            return new(null, 0, null);
        var u = encontrado;
        if (u.EsperarAte > instante)
            return new(null, 0, u.EsperarAte);

        if (SenhaHelper.Conferir(login.Senha, u.SenhaHash))
        {
            if (u.TentativasFalhas > 0 || u.EsperarAte is not null)
                await usuarios.RegistrarTentativas(u.IdUsuario, 0, null);
            return new(UsuarioService.ParaDto(u), u.VersaoSessao, null);
        }

        var tentativas = u.TentativasFalhas + 1;
        var esperarAte = Espera(tentativas) is { } espera ? instante + espera : (DateTime?)null;
        await usuarios.RegistrarTentativas(u.IdUsuario, tentativas, esperarAte);
        return new(null, 0, null);
    }

    /// <summary>Quanto a conta espera depois de tantas senhas erradas seguidas (nulo: ainda não espera).</summary>
    public static TimeSpan? Espera(int tentativas) => tentativas switch
    {
        < ErrosLivres => null,
        ErrosLivres => TimeSpan.FromSeconds(30),
        ErrosLivres + 1 => TimeSpan.FromMinutes(1),
        _ => TimeSpan.FromMinutes(5)
    };

    /// <summary>O token ainda vale? A conta tem de estar ativa e a versão da sessão do token tem de ser a atual (sair, desativar e trocar a senha sobem a versão).</summary>
    public async Task<bool> SessaoValida(int idUsuario, int versaoDoToken)
        => await usuarios.Sessao(idUsuario) is { } sessao && sessao.Ativo && sessao.VersaoSessao == versaoDoToken;

    /// <summary>Sair: encerra as sessões do usuário em todos os aparelhos.</summary>
    public Task Sair(int idUsuario) => usuarios.EncerrarSessoes(idUsuario);
}
