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
    /// <summary>O que a validação do token precisa a cada chamada: se a conta está ativa e a versão da sessão. Nulo se o usuário não existe.</summary>
    Task<(bool Ativo, int VersaoSessao)?> Sessao(int idUsuario);
    /// <summary>Sobe a versão da sessão: todo token já emitido para o usuário deixa de valer.</summary>
    Task EncerrarSessoes(int idUsuario);
    /// <summary>Ativa ou desativa a conta e encerra as sessões dela.</summary>
    Task DefinirAtivo(int idUsuario, bool ativo);
    /// <summary>Grava as senhas erradas seguidas e até quando a conta espera.</summary>
    Task RegistrarTentativas(int idUsuario, int tentativas, DateTime? esperarAte);
}
