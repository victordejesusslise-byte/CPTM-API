using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Comum;

namespace ViaPermanente.Api.Comum;

/// <summary>Regra de negócio quebrada vira 400 com a mensagem; o resto vira 500 sem detalhes internos.</summary>
public class ErroHandler(IProblemDetailsService problemas) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception erro, CancellationToken cancelamento)
    {
        if (erro is not RegraDeNegocioException)
            return false;

        contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = new ProblemDetails { Status = 400, Title = "Regra do sistema", Detail = erro.Message }
        });
    }
}