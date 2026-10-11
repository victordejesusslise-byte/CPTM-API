using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Testes.Services;

/// <summary>Repositório em memória para testar os services sem banco.</summary>
internal class UsuarioRepositoryFalso : IUsuarioRepository
{
    public List<UsuarioComPessoa> Linhas { get; } = [];

    public Task<UsuarioComPessoa?> BuscarPorEmail(string emailNormalizado)
        => Task.FromResult(Linhas.FirstOrDefault(x => x.Pessoa.Email == emailNormalizado));

    public Task<UsuarioComPessoa?> BuscarPorId(int idUsuario)
        => Task.FromResult(Linhas.FirstOrDefault(x => x.Usuario.IdUsuario == idUsuario));

    public Task<IReadOnlyList<UsuarioComPessoa>> Listar() => Task.FromResult<IReadOnlyList<UsuarioComPessoa>>(Linhas);

    public Task<bool> ExisteAlgum() => Task.FromResult(Linhas.Count > 0);

    public Task<bool> EmailEmUso(string emailNormalizado) => Task.FromResult(Linhas.Any(x => x.Pessoa.Email == emailNormalizado));

    public Task<UsuarioComPessoa> Criar(Pessoa pessoa, Usuario usuario)
    {
        pessoa.IdPessoa = Linhas.Count + 1;
        usuario.IdUsuario = Linhas.Count + 1;
        usuario.IdPessoa = pessoa.IdPessoa;
        var linha = new UsuarioComPessoa(usuario, pessoa);
        Linhas.Add(linha);
        return Task.FromResult(linha);
    }
}