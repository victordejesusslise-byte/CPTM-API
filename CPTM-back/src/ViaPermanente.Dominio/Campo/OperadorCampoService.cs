using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Dtos;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// O pacote do operador da via no app: os trechos, os alertas, as trocas e as passagens do carro dos últimos 7 ou 30 dias.
/// O celular guarda o pacote para abrir sem rede. O que ainda está aberto vai sempre, mesmo se for mais antigo.
/// </summary>
public class OperadorCampoService(TrechoMapaService mapa, AlertaService alertas, TrocaService trocas, PassagemConsultaService passagens)
{
    // As trocas que ainda não terminaram: vão no pacote mesmo se forem antigas.
    private static readonly string[] TrocasAbertas = ["Pedida", "Aprovada", "AguardandoPeca", "Separada", "Recusada"];

    /// <summary>Monta o pacote com o histórico dos últimos dias (7 ou 30).</summary>
    public async Task<PacoteDoOperadorDto> Montar(int dias, DateTime agora)
    {
        // Só duas opções: 30 dias, ou 7 para qualquer outro valor.
        dias = dias >= 30 ? 30 : 7;
        var desde = agora.AddDays(-dias);
        // Alertas: os abertos e os que nasceram ou foram decididos no período.
        var listaAlertas = (await alertas.Listar(null, null)).Where(a => a.Status == "Aberto" || a.Data >= desde || a.DataDecisao >= desde).ToList();
        // Trocas: as abertas e as que tiveram algum passo no período.
        var listaTrocas = (await trocas.Listar(null))
            .Where(t => TrocasAbertas.Contains(t.Status) || (t.Data ?? t.DataSeparacao ?? t.DataAprovacao ?? t.DataPedido) >= desde).ToList();
        // Passagens do carro que começaram no período.
        var listaPassagens = (await passagens.Ultimas(dias * 4)).Where(p => p.DataInicio >= desde).ToList();
        return new PacoteDoOperadorDto(dias, agora, await mapa.Trechos(), listaAlertas, listaTrocas, listaPassagens);
    }
}

/// <summary>O pacote do operador da via, como estava em GeradoEm, com o histórico dos últimos Dias (7 ou 30).</summary>
/// <param name="Dias">quantos dias de histórico: 7 ou 30.</param>
/// <param name="GeradoEm">a hora em que o pacote foi montado.</param>
/// <param name="Trechos">os trechos da via, com quantas peças estão em Ruim ou Crítico.</param>
/// <param name="Alertas">os alertas abertos e os do período.</param>
/// <param name="Trocas">as trocas abertas e as do período.</param>
/// <param name="Passagens">as passagens do carro no período.</param>
public record PacoteDoOperadorDto(int Dias, DateTime GeradoEm, IReadOnlyList<TrechoDoMapaDto> Trechos, IReadOnlyList<AlertaDto> Alertas,
    IReadOnlyList<TrocaDto> Trocas, IReadOnlyList<PassagemDto> Passagens);
