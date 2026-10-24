using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dominio.Pessoas;

public interface IUsuarioRepository
{
    Task<Usuario?> BuscarPorEmail(string emailNormalizado);
    Task<Usuario?> BuscarPorId(int idUsuario);
    Task<IReadOnlyList<Usuario>> Listar();
    Task<bool> ExisteAlgum();
    Task<bool> EmailEmUso(string emailNormalizado);
    /// <summary>Grava o usuário e o devolve com o id.</summary>
    Task<Usuario> Criar(Usuario usuario);
    /// <summary>O que a validação do token precisa a cada chamada: se a conta está ativa e a versão da sessão. Nulo se o usuário não existe.</summary>
    Task<(bool Ativo, int VersaoSessao)?> Sessao(int idUsuario);
    /// <summary>Sobe a versão da sessão: todo token já emitido para o usuário deixa de valer.</summary>
    Task EncerrarSessoes(int idUsuario);
    /// <summary>Ativa ou desativa a conta e encerra as sessões dela.</summary>
    Task DefinirAtivo(int idUsuario, bool ativo);
    /// <summary>Grava as senhas erradas seguidas e até quando a conta espera.</summary>
    Task RegistrarTentativas(int idUsuario, int tentativas, DateTime? esperarAte);
}
