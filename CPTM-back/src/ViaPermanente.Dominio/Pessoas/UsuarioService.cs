using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dominio.Pessoas;

/// <summary>
/// Cadastro dos usuários: cada um entra com o próprio e-mail e a própria senha. Quem usa: o Supervisor.
/// Regras: e-mail válido e único, senha de 8 caracteres ou mais (guardada só em hash) e ninguém desativa a própria conta.
/// </summary>
public class UsuarioService(IUsuarioRepository usuarios)
{
    /// <summary>O mínimo de caracteres da senha.</summary>
    public const int TamanhoMinimoSenha = 8;

    /// <summary>Confere os dados, grava a conta já ativa, com a senha em hash, e devolve o usuário criado.</summary>
    public async Task<UsuarioDto> Criar(NovoUsuarioDto novo)
    {
        // O e-mail é comparado sempre normalizado, para "Ana@X.com" e "ana@x.com" serem a mesma conta.
        var email = EmailHelper.Normalizar(novo.Email);
        if (string.IsNullOrWhiteSpace(novo.Nome))
            throw new RegraDeNegocioException("Informe o nome.");
        if (!EmailHelper.PareceValido(email))
            throw new RegraDeNegocioException("E-mail inválido.");
        if (novo.Senha.Length < TamanhoMinimoSenha)
            throw new RegraDeNegocioException($"A senha precisa ter pelo menos {TamanhoMinimoSenha} caracteres.");
        if (novo.Cargo is null)
            throw new RegraDeNegocioException("Informe o cargo.");
        // O e-mail é o login: dois usuários com o mesmo e-mail não teriam como entrar.
        if (await usuarios.EmailEmUso(email))
            throw new RegraDeNegocioException("Já existe um usuário com este e-mail.");

        // A senha nunca é guardada: só o hash dela.
        var criado = await usuarios.Criar(
            new Usuario
            {
                Nome = novo.Nome.Trim(),
                Email = email,
                TipoCargo = novo.Cargo.Value,
                DataCadastro = DateTime.Now,
                SenhaHash = SenhaHelper.GerarHash(novo.Senha),
                Ativo = true
            });
        return ParaDto(criado);
    }

    /// <summary>Todas as contas, já sem a senha.</summary>
    public async Task<IReadOnlyList<UsuarioDto>> Listar()
        => (await usuarios.Listar()).Select(ParaDto).ToList();

    /// <summary>Uma conta pelo id (→ USUARIO); nulo se não existe.</summary>
    public async Task<UsuarioDto?> BuscarPorId(int idUsuario)
    {
        var encontrado = await usuarios.BuscarPorId(idUsuario);
        return encontrado is null ? null : ParaDto(encontrado);
    }

    /// <summary>
    /// Na primeira subida, sem nenhum usuário no banco, cria o supervisor com o e-mail e a senha do .env.
    /// Devolve verdadeiro se criou.
    /// </summary>
    public async Task<bool> GarantirSupervisorInicial(string? nome, string? email, string? senha)
    {
        // Sem e-mail ou senha no .env, ou se o banco já tem alguém, não faz nada.
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha) || await usuarios.ExisteAlgum())
            return false;

        // Passa pelas mesmas regras de qualquer cadastro.
        await Criar(new NovoUsuarioDto
        {
            Nome = string.IsNullOrWhiteSpace(nome) ? "Supervisor" : nome,
            Email = email,
            Senha = senha,
            Cargo = Cargo.Supervisor
        });
        return true;
    }

    /// <summary>Desativa ou reativa a conta; desativar derruba na hora as sessões abertas. Ninguém desativa a própria conta. Nulo se não existe.</summary>
    public async Task<UsuarioDto?> DefinirAtivo(int idUsuario, bool ativo, int idQuemFaz)
    {
        // Um supervisor que se desativasse poderia deixar o sistema sem ninguém para cadastrar.
        if (!ativo && idUsuario == idQuemFaz)
            throw new RegraDeNegocioException("Você não pode desativar a sua própria conta.");
        if (await usuarios.BuscarPorId(idUsuario) is null)
            return null;
        await usuarios.DefinirAtivo(idUsuario, ativo);
        return await BuscarPorId(idUsuario);
    }

    /// <summary>A conta como sai da API: sem a senha; o EsperarAte só aparece enquanto a espera ainda vale.</summary>
    internal static UsuarioDto ParaDto(Usuario u)
        => new(u.IdUsuario, u.Nome, u.Email, u.TipoCargo.ToString(), u.Ativo, u.EsperarAte > DateTime.Now ? u.EsperarAte : null);
}