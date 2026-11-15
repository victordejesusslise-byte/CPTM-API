
namespace ViaPermanente.Dominio.Comum;

/// <summary>Uma regra do sistema impediu a operação. A API devolve 400 com a mensagem, e a tela mostra essa mensagem
/// ao usuário; por isso ela é sempre uma frase clara em português.</summary>
public class RegraDeNegocioException(string mensagem) : Exception(mensagem);