using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Comum;

namespace ViaPermanente.Api.Comum;

/// <summary>Regra de negócio quebrada vira 400 com a mensagem; o resto vira 500 sem detalhes internos.</summary>
public class ErroHandler(IProblemDetailsService problemas) : IExceptionHandler
{
    /// <summary>Roda em todo erro: se for regra de negócio, responde 400 com a mensagem; senão, deixa para o padrão (500).</summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception erro, CancellationToken cancelamento)
    {
        // Erro que não é regra de negócio: não é nosso, deixa para o tratamento padrão.
        if (erro is not RegraDeNegocioException)
            return false;

        // A mensagem da regra já é escrita para o usuário ler.
        contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = new ProblemDetails { Status = 400, Title = "Regra do sistema", Detail = erro.Message }
        });
    }
}