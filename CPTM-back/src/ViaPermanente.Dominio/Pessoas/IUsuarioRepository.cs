using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dominio.Pessoas;

public interface IUsuarioRepository
{
    Task<UsuarioComPessoa?> BuscarPorEmail(string emailNormalizado);
    Task<UsuarioComPessoa?> BuscarPorId(int idUsuario);
    Task<IReadOnlyList<UsuarioComPessoa>> Listar();
    Task<bool> ExisteAlgum();
    Task<bool> EmailEmUso(string emailNormalizado);
    /// <summary>Grava a pessoa e o usuário juntos (numa transação) e devolve os dois com os ids.</summary>
    Task<UsuarioComPessoa> Criar(Pessoa pessoa, Usuario usuario);
}