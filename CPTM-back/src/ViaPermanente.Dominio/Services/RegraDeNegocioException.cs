namespace ViaPermanente.Dominio.Services;

/// <summary>Uma regra do sistema impediu a operação. A API devolve 400 com a mensagem.</summary>
public class RegraDeNegocioException(string mensagem) : Exception(mensagem);