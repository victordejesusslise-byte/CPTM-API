using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ViaPermanente.Dominio.Campo.Dtos;

namespace ViaPermanente.Api.Campo;

/// <summary>
/// O ao vivo do campo (app Android, Etapa 4, 2026-10-24): o celular logado se conecta em /hub/campo e o servidor avisa quando o
/// trabalho da noite muda. O aviso leva só o ID; o celular busca de novo o plano (GET /api/campo/plano), que é o que vale.
/// </summary>
[Authorize]
public class CampoHub : Hub
{
}

/// <summary>Manda os avisos do ao vivo. Chamado pelos handlers depois que o registro foi gravado.</summary>
public class AvisoAoVivo(IHubContext<CampoHub> hub, ILogger<AvisoAoVivo> log)
{
    /// <summary>Uma troca do plano mudou: o celular baixa o plano de novo.</summary>
    public const string TrocaNoPlano = "trocaNoPlano";
    /// <summary>Uma troca foi aprovada como urgente.</summary>
    public const string TrocaUrgente = "trocaUrgente";
    /// <summary>O técnico pediu uma troca: o aviso para quem aprova (Victor, 2026-10-31).</summary>
    public const string TrocaPedida = "trocaPedida";
    /// <summary>A passagem abriu alertas novos.</summary>
    public const string AlertaNovo = "alertaNovo";
    /// <summary>Um alerta foi fechado (descartado ou confirmado pela inspeção).</summary>
    public const string AlertaFechado = "alertaFechado";
    /// <summary>O técnico pediu um lote de troca: o aviso para quem aprova (Victor, 2026-11-11).</summary>
    public const string LotePedido = "lotePedido";
    /// <summary>Um lote de troca do plano mudou.</summary>
    public const string LoteNoPlano = "loteNoPlano";

    /// <summary>A troca mudou (aprovada, separada, feita, recusada, cancelada). Aprovada como urgente vai como urgente.</summary>
    public Task Troca(TrocaDto? troca) => troca is null
        ? Task.CompletedTask
        : Avisar(troca.Urgente && troca.Status == "Aprovada" ? TrocaUrgente : TrocaNoPlano, troca.Id);

    /// <summary>O lote mudou (aprovado, separado, feito, recusado, cancelado): o celular baixa o plano de novo.</summary>
    public Task Lote(LoteTrocaDto? lote) => lote is null ? Task.CompletedTask : Avisar(LoteNoPlano, lote.Id);

    /// <summary>Manda o aviso (o nome do evento e o id) para todos os celulares conectados.</summary>
    public async Task Avisar(string evento, int? id)
    {
        // O aviso é um extra: se falhar, o registro já está gravado e o celular pega a mudança na próxima busca do plano.
        try
        {
            await hub.Clients.All.SendAsync(evento, new { id });
        }
        catch (Exception e)
        {
            log.LogWarning(e, "Não deu para mandar o aviso ao vivo {Evento} ({Id}).", evento, id);
        }
    }
}
