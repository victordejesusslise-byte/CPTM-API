using Microsoft.AspNetCore.Mvc;

namespace ViaPermanente.Api.Comum;

/// <summary>
/// A versão mais nova do app Android e onde baixar (App:Versao e App:Baixar; no Docker, App__Versao). O app compara com a dele e
/// avisa que tem versão nova. A cada APK novo publicado, atualizar App:Versao.
/// </summary>
[ApiController]
[Route("api/app")]
public class AppHandler(IConfiguration config) : ControllerBase
{
    /// <summary>GET /api/app/versao (qualquer cargo logado): a versão anunciada e o link do APK (vazios se não configurados).</summary>
    [HttpGet("versao")]
    public object Versao() => new { versao = config["App:Versao"] ?? "", baixar = config["App:Baixar"] ?? "" };
}
