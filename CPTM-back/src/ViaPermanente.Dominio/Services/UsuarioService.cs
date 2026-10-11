using ViaPermanente.Dominio.Dtos.Usuarios;
using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dominio.Services;

/// <summary>Cadastro dos usuários: cada um entra com o próprio e-mail e a própria senha.</summary>
public class UsuarioService(IUsuarioRepository usuarios)
{
    public const int TamanhoMinimoSenha = 8;

    public async Task<UsuarioDto> Criar(NovoUsuarioDto novo)
    {
        var email = EmailHelper.Normalizar(novo.Email);
        if (string.IsNullOrWhiteSpace(novo.Nome))
            throw new RegraDeNegocioException("Informe o nome.");
        if (!EmailHelper.PareceValido(email))
            throw new RegraDeNegocioException("E-mail inválido.");
        if (novo.Senha.Length < TamanhoMinimoSenha)
            throw new RegraDeNegocioException($"A senha precisa ter pelo menos {TamanhoMinimoSenha} caracteres.");
        if (novo.Cargo is null)
            throw new RegraDeNegocioException("Informe o cargo.");
        if (await usuarios.EmailEmUso(email))
            throw new RegraDeNegocioException("Já existe um usuário com este e-mail.");

        var criado = await usuarios.Criar(
            new Pessoa { Nome = novo.Nome.Trim(), Email = email },
            new Usuario
            {
                TipoCargo = novo.Cargo.Value,
                DataCadastro = DateTime.Now,
                SenhaHash = SenhaHelper.GerarHash(novo.Senha),
                Ativo = true
            });
        return ParaDto(criado);
    }

    public async Task<IReadOnlyList<UsuarioDto>> Listar()
        => (await usuarios.Listar()).Select(ParaDto).ToList();

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
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha) || await usuarios.ExisteAlgum())
            return false;

        await Criar(new NovoUsuarioDto
        {
            Nome = string.IsNullOrWhiteSpace(nome) ? "Supervisor" : nome,
            Email = email,
            Senha = senha,
            Cargo = Cargo.Supervisor
        });
        return true;
    }

    internal static UsuarioDto ParaDto(UsuarioComPessoa u)
        => new(u.Usuario.IdUsuario, u.Pessoa.Nome, u.Pessoa.Email ?? "", u.Usuario.TipoCargo.ToString(), u.Usuario.Ativo);
}