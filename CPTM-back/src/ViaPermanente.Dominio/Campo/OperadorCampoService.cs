using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Dtos;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// O pacote do operador da via (app, ampliação da Entrega 2, Victor, 2026-10-26): o que ele consulta no trecho, para o celular guardar e
/// abrir sem rede. Os trechos (o mapa o celular monta com a cópia das linhas), os alertas e as trocas dos últimos dias (mais os abertos)
/// e o resumo das passagens do carro. A pessoa escolhe no app 7 ou 30 dias.
/// </summary>
public class OperadorCampoService(TrechoMapaService mapa, AlertaService alertas, TrocaService trocas, PassagemConsultaService passagens)
{
    private static readonly string[] TrocasAbertas = ["Pedida", "Aprovada", "AguardandoPeca", "Separada", "Recusada"];

    public async Task<PacoteDoOperadorDto> Montar(int dias, DateTime agora)
    {
        dias = dias >= 30 ? 30 : 7;
        var desde = agora.AddDays(-dias);
        var listaAlertas = (await alertas.Listar(null, null)).Where(a => a.Status == "Aberto" || a.Data >= desde || a.DataDecisao >= desde).ToList();
        var listaTrocas = (await trocas.Listar(null))
            .Where(t => TrocasAbertas.Contains(t.Status) || (t.Data ?? t.DataSeparacao ?? t.DataAprovacao ?? t.DataPedido) >= desde).ToList();
        var listaPassagens = (await passagens.Ultimas(dias * 4)).Where(p => p.DataInicio >= desde).ToList();
        return new PacoteDoOperadorDto(dias, agora, await mapa.Trechos(), listaAlertas, listaTrocas, listaPassagens);
    }
}

/// <summary>O pacote do operador da via, como estava em GeradoEm, com o histórico dos últimos Dias (7 ou 30).</summary>
public record PacoteDoOperadorDto(int Dias, DateTime GeradoEm, IReadOnlyList<TrechoDoMapaDto> Trechos, IReadOnlyList<AlertaDto> Alertas,
    IReadOnlyList<TrocaDto> Trocas, IReadOnlyList<PassagemDto> Passagens);
