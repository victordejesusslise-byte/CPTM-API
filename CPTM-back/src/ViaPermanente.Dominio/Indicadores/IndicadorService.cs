using System.Globalization;
using ViaPermanente.Dominio.Ambiental;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Indicadores.Dtos;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos;

namespace ViaPermanente.Dominio.Indicadores;

/// <summary>
/// Os 18 indicadores do cérebro, calculados das tabelas de evento para o período (os de "agora" ignoram o período:
/// estoque, dias de peça condenada, última conferência e falhas da tag em 7 noites). Sem dado, o valor vem nulo.
/// </summary>
public class IndicadorService(IIndicadorRepository dados, IRelatorioAmbientalRepository estoque, PedidoService pedidos,
    ICatalogoRepository catalogo, ICadastroRepository cadastros)
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly StatusTroca[] Abertas = [StatusTroca.Pedida, StatusTroca.Aprovada, StatusTroca.AguardandoPeca, StatusTroca.Separada, StatusTroca.Recusada];
    public const int LimiteInspecaoSegundos = 1800;

    public async Task<IReadOnlyList<IndicadorDto>> Calcular(DateTime? de, DateTime? ate, DateTime agora)
    {
        var fim = ate ?? agora;
        var inicio = de ?? fim.AddDays(-30);
        var periodo = $"de {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}";
        var alertas = await dados.AlertasDecididos(inicio, fim);
        var inspecoes = await dados.Inspecoes(inicio, fim);
        var trocas = await dados.Trocas();
        var modos = (await catalogo.ListarModos()).ToDictionary(m => m.IdModo, m => m.Nome);
        var empresas = (await cadastros.Empresas()).ToDictionary(e => e.IdEmpresa, e => e.Nome);
        var armazens = (await catalogo.ListarArmazens()).ToDictionary(a => a.IdArmazem, a => a.Nome);
        var especies = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie, e => e.NomeCientifico);
        var lista = new List<IndicadorDto>();

        // 1 e 4: a câmera e os sensores do carro
        var imagem = alertas.Where(a => a.Origem == OrigemAlerta.Imagem).ToList();
        lista.Add(Taxa("acerto-camera", "Acerto da câmera", imagem.Count(a => a.Status == StatusAlerta.Confirmado), imagem.Count,
            "alertas de imagem confirmados entre os decididos", "a definir com os primeiros meses", null,
            [.. imagem.GroupBy(a => a.Tipo.ToString()).Select(g => Det($"defeito {g.Key}", Pct(g.Count(a => a.Status == StatusAlerta.Confirmado), g.Count()))),
             .. imagem.GroupBy(a => a.VersaoModelo ?? "?").Select(g => Det($"modelo {g.Key}", Pct(g.Count(a => a.Status == StatusAlerta.Confirmado), g.Count())))]));
        var sensor = alertas.Where(a => a.Origem == OrigemAlerta.Sensor).ToList();
        lista.Add(Taxa("confiabilidade-dados", "Confiabilidade dos dados", sensor.Count(a => a.Status == StatusAlerta.Confirmado), sensor.Count,
            "alertas de sensor confirmados entre os decididos", "vermelho abaixo de 70%", v => v < 70,
            sensor.GroupBy(a => a.Tipo.ToString()).Select(g => Det(g.Key, Pct(g.Count(a => a.Status == StatusAlerta.Confirmado), g.Count()))).ToList()));

        // 2: OCR
        var ocr = await dados.CamposOcrConfirmados(inicio, fim);
        lista.Add(Taxa("acerto-ocr", "Acerto do OCR", ocr.Count(c => !c.Corrigido), ocr.Count, "campos confirmados sem correção", "a definir com os primeiros meses", null,
            [.. ocr.GroupBy(c => c.Campo).Select(g => Det(g.Key, Pct(g.Count(c => !c.Corrigido), g.Count()))),
             .. ocr.GroupBy(c => c.Versao).Select(g => Det($"modelo v{g.Key}", Pct(g.Count(c => !c.Corrigido), g.Count())))]));

        // 3: alvo único
        var leituras = await dados.LeiturasDoModulo(inicio, fim);
        lista.Add(Taxa("alvo-unico", "Alvo único na leitura", leituras.Count(l => !l.Ambigua), leituras.Count, "leituras do módulo com alvo único",
            "a definir nos ensaios", null,
            leituras.GroupBy(l => l.IdModo is int m ? modos.GetValueOrDefault(m, $"modo {m}") : "sem modo")
                .Select(g => Det(g.Key, Pct(g.Count(l => !l.Ambigua), g.Count()))).ToList()));

        // 5 e 17: troca
        var condenadas = trocas.Where(t => Abertas.Contains(t.Status)).Select(t => (agora - t.DataPedido).TotalDays).ToList();
        lista.Add(new("dias-peca-condenada", "Dias de peça condenada na via", condenadas.Count == 0 ? null : Arred((decimal)condenadas.Average()), "dias",
            $"{condenadas.Count} troca(s) aberta(s); média de dias desde o pedido", null, false,
            condenadas.Count == 0 ? [] : [Det("maior espera", $"{condenadas.Max():0.#} dias")]));
        var noPeriodo = trocas.Where(t => t.DataPedido >= inicio && t.DataPedido < fim).ToList();
        decimal? Media(IEnumerable<double> dias) => dias.ToList() is { Count: > 0 } d ? Arred((decimal)d.Average()) : null;
        var aprovar = Media(noPeriodo.Where(t => t.DataAprovacao != null).Select(t => (t.DataAprovacao!.Value - t.DataPedido).TotalDays));
        var separar = Media(noPeriodo.Where(t => t.DataAprovacao != null && t.DataSeparacao != null).Select(t => (t.DataSeparacao!.Value - t.DataAprovacao!.Value).TotalDays));
        var fazer = Media(noPeriodo.Where(t => t.DataSeparacao != null && t.Data != null).Select(t => (t.Data!.Value - t.DataSeparacao!.Value).TotalDays));
        lista.Add(new("tempo-passos-troca", "Tempo em cada passo da troca", aprovar is null && separar is null && fazer is null ? null : (aprovar ?? 0) + (separar ?? 0) + (fazer ?? 0),
            "dias", $"{noPeriodo.Count} troca(s) pedida(s) {periodo}; soma das médias dos passos", null, false,
            [Det("pedida → aprovada", Dias(aprovar)), Det("aprovada → separada", Dias(separar)), Det("separada → feita", Dias(fazer))]));

        // 6: fora do banco
        lista.Add(new("disponibilidade-registros", "Disponibilidade dos registros", null, "%", "fora do banco: é monitoramento do servidor", null, false, []));

        // 7: nota × chegada
        var chegadas = await dados.Chegadas(inicio, fim);
        var divergentes = chegadas.Where(c => ConferenciaChegada.Conferir(c.Nota, c.Informada, [c.Feixes]) is not null).ToList();
        lista.Add(Taxa("divergencia-nota-chegada", "Divergência entre nota e chegada", divergentes.Count, chegadas.Count,
            "chegadas em que o informado não bate com a nota ou com os feixes", null, null, divergentes.Select(c => Det($"chegada {c.IdRecebimento}", $"informado {c.Informada}, nota {c.Nota?.ToString() ?? "—"}, feixes {c.Feixes}")).ToList()));

        // 8: conferência do armazém
        var conferencias = await dados.UltimasConferenciasConcluidas();
        var esperadas = conferencias.Sum(c => c.Esperadas ?? 0);
        lista.Add(Taxa("divergencia-conferencia", "Divergência na conferência do armazém", conferencias.Sum(c => (c.Faltando ?? 0) + (c.AMais ?? 0)), esperadas,
            "(faltando + a mais) ÷ esperadas, na última conferência de cada armazém", "a definir", null,
            conferencias.Select(c => Det(armazens.GetValueOrDefault(c.IdArmazem, $"armazém {c.IdArmazem}"),
                $"{Pct(((c.Faltando ?? 0) + (c.AMais ?? 0)), c.Esperadas ?? 0)} ({c.Faltando} faltando, {c.AMais} a mais, {c.ConcluidaEm:dd/MM})")).ToList()));

        // 9: estoque
        var pecas = (await estoque.Pecas()).Where(p => p.EntradaEm is not null && p.SaidaEm is null).ToList();
        lista.Add(new("estoque-m3", "Estoque em m³ por espécie e tempo", Arred(pecas.Sum(p => p.M3), 3), "m³",
            $"{pecas.Count} peça(s) aprovadas no armazém", "mensal (base do relatório ambiental)", false,
            pecas.GroupBy(p => p.IdEspecie).Select(g => Det(especies.GetValueOrDefault(g.Key, $"espécie {g.Key}"),
                $"{g.Sum(p => p.M3).ToString("0.###", Br)} m³ · idade média {g.Average(p => (agora - p.EntradaEm!.Value).TotalDays):0} dias")).ToList()));

        // 10: falhas da tag
        var falhas = await dados.FalhasDeTag(agora.AddDays(-7));
        var quebradas = falhas.Where(f => f.Noites >= 3).ToList();
        lista.Add(new("falhas-tag-7-noites", "Falhas da mesma tag em 7 noites", quebradas.Count, "tags",
            $"tags que falharam em 3 ou mais das últimas 7 noites (de {falhas.Count} com alguma falha)", "3 de 7 é tag quebrada", quebradas.Count > 0,
            quebradas.Select(f => Det($"peça {f.IdDormente}", $"{f.Noites} noites")).ToList()));

        // 11: gravação
        var gravacoes = (await dados.Gravacoes(inicio, fim)).Where(r => r != ResultadoGravacao.Bloqueada).ToList();
        lista.Add(Taxa("gravacao-conferida", "Gravação conferida", gravacoes.Count(r => r == ResultadoGravacao.Ok), gravacoes.Count,
            "gravações com a releitura igual (sem as bloqueadas antes de gravar)", "a definir; uma divergente já pede olhar a tag", null,
            gravacoes.GroupBy(r => r).Select(g => Det(g.Key.ToString(), g.Count().ToString())).ToList()));

        // 12, 16 e 18: inspeção
        var dias = Math.Max(1, (int)Math.Ceiling((fim - inicio).TotalDays));
        lista.Add(new("inspecoes-por-dia", "Inspeções por dia", Arred(inspecoes.Count / (decimal)dias), "inspeções/dia", $"{inspecoes.Count} inspeção(ões) {periodo}", null, false,
            inspecoes.GroupBy(i => i.FinalizadoEm.Date).OrderBy(g => g.Key).Select(g => Det($"{g.Key:dd/MM}", g.Count().ToString())).ToList()));
        lista.Add(Taxa("taxa-retrabalho", "Taxa de retrabalho", inspecoes.Count(i => i.IdInspecaoOrigem != null), inspecoes.Count, "inspeções que refazem outra", null, null, []));
        var validas = inspecoes.Where(i => i.DuracaoS <= LimiteInspecaoSegundos).ToList();
        lista.Add(new("tempo-medio-inspecao", "Tempo médio de inspeção", validas.Count == 0 ? null : Arred((decimal)validas.Average(i => i.DuracaoS) / 60), "min",
            $"{validas.Count} inspeção(ões); {inspecoes.Count - validas.Count} acima de 30 min ficaram fora como suspeitas", "meta do plano: 4,9 min por dormente",
            validas.Count > 0 && validas.Average(i => i.DuracaoS) / 60 > 4.9, []));

        // 13: classificação
        var revisoes = await dados.RevisoesDaClassificacaoVisual(inicio, fim);
        lista.Add(Taxa("identificado-corretamente", "Percentual identificado corretamente", revisoes.Count(c => c), revisoes.Count,
            "revisões por laboratório ou catálogo que confirmaram a classificação visual", "vermelho abaixo de 90%", v => v < 90, []));

        // 14: reprovação por fornecedor
        var verificacoes = await dados.VerificacoesPorFornecedor(inicio, fim);
        lista.Add(Taxa("reprovacao-fornecedor", "Reprovação na chegada por fornecedor", verificacoes.Count(v => v.Reprovada), verificacoes.Count,
            "peças reprovadas na verificação de qualidade", null, null,
            verificacoes.GroupBy(v => v.IdEmpresa).Select(g => Det(empresas.GetValueOrDefault(g.Key, $"empresa {g.Key}"),
                $"{Pct(g.Count(v => v.Reprovada), g.Count())} ({g.Count(v => v.Reprovada)} de {g.Count()})")).ToList()));

        // 15: 2ª classe
        var dosPedidos = (await pedidos.Listar()).Where(p => p.CriadoEm >= inicio && p.CriadoEm < fim && p.Situacao != "Cancelado").ToList();
        lista.Add(new("segunda-classe-pedido", "Segunda classe por pedido", dosPedidos.Count == 0 ? null : Arred(dosPedidos.Average(p => p.SegundaClasse.Percentual)), "%",
            $"média de {dosPedidos.Count} pedido(s) {periodo}", dosPedidos.FirstOrDefault() is { } p0 ? $"limite de {p0.SegundaClasse.Limite:0.#}% em {p0.SegundaClasse.Unidade}" : "limite em PARAMETRO",
            dosPedidos.Any(p => !p.SegundaClasse.Valido), dosPedidos.Select(p => Det(p.Codigo, $"{p.SegundaClasse.Percentual:0.#}%")).ToList()));

        return lista;
    }

    private static IndicadorDto Taxa(string chave, string nome, int parte, int total, string resumo, string? referencia, Func<decimal, bool>? vermelho,
        IReadOnlyList<DetalheIndicadorDto> detalhes)
    {
        decimal? valor = total == 0 ? null : Arred(parte * 100m / total);
        return new(chave, nome, valor, "%", total == 0 ? $"sem dado no período ({resumo})" : $"{parte} de {total}: {resumo}", referencia,
            valor is decimal v && vermelho?.Invoke(v) == true, detalhes);
    }

    private static DetalheIndicadorDto Det(string rotulo, string valor) => new(rotulo, valor);
    private static string Pct(int parte, int total) => total == 0 ? "—" : $"{parte * 100m / total:0.#}%";
    private static string Dias(decimal? d) => d is null ? "—" : $"{d:0.#} dias";
    private static decimal Arred(decimal v, int casas = 1) => Math.Round(v, casas);
}