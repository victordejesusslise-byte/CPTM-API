using Microsoft.AspNetCore.Mvc;

namespace ViaPermanente.Api.Comum;

/// <summary>
/// A versão mais nova do app Android e onde baixar (App:Versao e App:Baixar; no Docker, App__Versao). O app compara com a dele e
/// avisa que tem versão nova, só se a dele for mais velha. A cada APK novo publicado, atualizar a versão anunciada: no PC, em
/// CPTM-back/docker-compose.override.yml (fora do Git, para a mudança não gerar outro APK) e reiniciar o back; os celulares com o app
/// aberto reconectam e conferem de novo (Victor, 2026-10-31).
/// </summary>
[ApiController]
[Route("api/app")]
public class AppHandler(IConfiguration config) : ControllerBase
{
    [HttpGet("versao")]
    public object Versao() => new { versao = config["App:Versao"] ?? "", baixar = config["App:Baixar"] ?? "" };
}
