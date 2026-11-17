using System.Globalization;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Via.Dtos;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Via;

/// <summary>
/// Etapa 08: a passagem noturna do carro. Grava as leituras, as detecções da câmera e o caminho do GPS; com as duas estações,
/// calcula o km de cada uma pelo GPS. Abre os alertas, atualiza a posição de cada peça e grava o evento quando ela muda de faixa.
/// </summary>
public class VarreduraService(IVarreduraRepository varredura, IParametroRepository parametros)
{
    public const string ChaveLimite = "desvio_limite_mm";
    public const string ChaveAtencao = "desvio_atencao_mm";
    public const string ChaveConfianca = "alerta_imagem_confianca_minima";

    public async Task<PassagemDto> Registrar(NovaPassagemDto nova)
    {
        var codigo = (nova.Codigo ?? "").Trim();
        if (codigo.Length == 0)
            throw new RegraDeNegocioException("Informe o código da passagem.");
        if (await varredura.CodigoEmUso(codigo))
            throw new RegraDeNegocioException($"A passagem {codigo} já foi registrada.");
        if (nova.Leituras.Count == 0 && nova.Deteccoes.Count == 0)
            throw new RegraDeNegocioException("A passagem não trouxe leitura nem detecção.");
        if (nova.Deteccoes.Any(d => d.Confianca is < 0 or > 1 || d.Grau is < 1 or > 3 || string.IsNullOrWhiteSpace(d.QuadroArquivo)))
            throw new RegraDeNegocioException("Cada detecção precisa do quadro, de grau 1 a 3 e de confiança de 0 a 1.");
        if (CoordenadaInvalida(nova))
            throw new RegraDeNegocioException("A latitude vai de −90 a 90 e a longitude de −180 a 180.");

        // As estações da passagem (Victor, 2026-11-17): a A é o começo (o bip 0) e a B o fim. O km delas vem do cadastro, ninguém digita.
        var (kmInicio, kmFim) = await KmDasEstacoes(nova);

        var limite = await Numero(ChaveLimite);
        var atencao = await Numero(ChaveAtencao);
        var confiancaMinima = await Numero(ChaveConfianca);
        var tags = nova.Leituras.Select(l => l.Tag).Concat(nova.Deteccoes.Select(d => d.Tag))
            .Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!.Trim()).Distinct().ToList();
        var donos = await varredura.DonosDasTags(tags);
        int? Dono(string? tag, int? idDormente) => idDormente ?? (tag is not null && donos.TryGetValue(tag.Trim(), out var id) ? id : null);

        var passagem = new Passagem
        {
            Codigo = codigo, Veiculo = nova.Veiculo, DataInicio = nova.DataInicio, DataFim = nova.DataFim, KmInicio = kmInicio, KmFim = kmFim,
            IdEstacaoInicio = nova.IdEstacaoInicio, IdEstacaoFim = nova.IdEstacaoFim
        };
        var leituras = nova.Leituras.Select(l => new LeituraVia
        {
            IdDormente = Dono(l.Tag, l.IdDormente), Leitura = l.Leitura, KmMedido = l.KmMedido, DesvioMm = l.DesvioMm,
            Data = l.Data == default ? nova.DataInicio : l.Data, Umidade = l.Umidade, Temperatura = l.Temperatura, RachaduraSuspeita = l.RachaduraSuspeita,
            Latitude = l.Latitude, Longitude = l.Longitude
        }).ToList();
        var deteccoes = nova.Deteccoes.Select(d => new DeteccaoImagem
        {
            IdDormente = Dono(d.Tag, null), KmMedido = d.KmMedido, QuadroArquivo = d.QuadroArquivo.Trim(), Camera = d.Camera,
            CaixaX = d.CaixaX, CaixaY = d.CaixaY, CaixaLargura = d.CaixaLargura, CaixaAltura = d.CaixaAltura,
            Defeito = d.Defeito, Grau = d.Grau, Confianca = d.Confianca, VersaoModelo = d.VersaoModelo, Data = d.Data == default ? nova.DataInicio : d.Data,
            Latitude = d.Latitude, Longitude = d.Longitude
        }).ToList();
        var pontos = nova.Pontos.Select(p => new PontoPassagem { Data = p.Data, Latitude = p.Latitude, Longitude = p.Longitude }).ToList();

        // Com as duas estações, o km de cada ponto, leitura e detecção sai do caminho que o GPS andou e vale no lugar do que o carro mandar.
        if (nova.IdEstacaoInicio is not null)
            CalcularKm(kmInicio!.Value, kmFim!.Value, pontos, leituras, deteccoes);

        var resumo = (Sensor: 0, Imagem: 0, Mudancas: 0);
        await varredura.EmTransacao(async () =>
        {
            await varredura.GravarPassagem(passagem, leituras, deteccoes, pontos);
            var pecas = leituras.Select(l => l.IdDormente).Concat(deteccoes.Select(d => d.IdDormente)).OfType<int>().Distinct().ToList();
            var abertos = (await varredura.AlertasAbertos(pecas)).Select(a => (a.IdDormente, a.Origem, a.Tipo)).ToHashSet();
            var alertas = new List<Alerta>();

            foreach (var l in leituras.Where(l => l.IdDormente is not null))
                if (PosicaoNaVia.AlertaDaLeitura(l.Leitura, l.RachaduraSuspeita, l.DesvioMm, limite) is TipoAlerta tipo
                    && abertos.Add((l.IdDormente, OrigemAlerta.Sensor, tipo)))
                    alertas.Add(new Alerta
                    {
                        Origem = OrigemAlerta.Sensor, IdLeituraVia = l.IdLeituraVia, IdDormente = l.IdDormente, Tipo = tipo,
                        DesvioMm = l.DesvioMm, Data = l.Data, Status = StatusAlerta.Aberto
                    });
            resumo.Sensor = alertas.Count;

            foreach (var d in deteccoes.Where(d => d.Confianca >= confiancaMinima))
            {
                var tipo = PosicaoNaVia.TipoDoDefeito(d.Defeito);
                if (d.IdDormente is null || abertos.Add((d.IdDormente, OrigemAlerta.Imagem, tipo)))
                    alertas.Add(new Alerta
                    {
                        Origem = OrigemAlerta.Imagem, IdDeteccao = d.IdDeteccao, IdDormente = d.IdDormente, Tipo = tipo,
                        Data = d.Data, Status = StatusAlerta.Aberto
                    });
            }
            resumo.Imagem = alertas.Count - resumo.Sensor;

            var atuais = await varredura.Posicoes(pecas);
            var novas = new List<PosicaoAtual>();
            var alteradas = new List<PosicaoAtual>();
            var eventos = new List<EventoPosicao>();
            foreach (var l in leituras.Where(l => l.IdDormente is not null && l.Leitura && l.DesvioMm is not null)
                         .GroupBy(l => l.IdDormente!.Value).Select(g => g.OrderBy(l => l.Data).Last()))
            {
                var faixa = PosicaoNaVia.Faixa(l.DesvioMm!.Value, atencao, limite);
                var idDormente = l.IdDormente!.Value;
                var antes = atuais.GetValueOrDefault(idDormente);
                if ((antes is null && faixa != FaixaPosicao.Ok) || (antes is not null && antes.Faixa != faixa))
                    eventos.Add(new EventoPosicao
                    {
                        IdDormente = idDormente, IdPassagem = passagem.IdPassagem, FaixaAnterior = antes?.Faixa, FaixaNova = faixa,
                        DesvioMm = l.DesvioMm, Data = l.Data
                    });
                var posicao = new PosicaoAtual
                {
                    IdDormente = idDormente, IdPassagem = passagem.IdPassagem, KmMedido = l.KmMedido, DesvioMm = l.DesvioMm, Faixa = faixa, AtualizadoEm = l.Data
                };
                (antes is null ? novas : alteradas).Add(posicao);
            }
            resumo.Mudancas = eventos.Count;
            await varredura.GravarResultado(alertas, novas, alteradas, eventos);
        });

        return new PassagemDto(passagem.IdPassagem, passagem.Codigo, passagem.DataInicio, passagem.DataFim,
            leituras.Count, leituras.Count(l => !l.Leitura),
            leituras.Count(l => l.Leitura && l.DesvioMm is decimal d && Math.Abs(d) > limite),
            nova.Leituras.Count(l => l.IdDormente is null && !string.IsNullOrWhiteSpace(l.Tag) && !donos.ContainsKey(l.Tag.Trim())),
            resumo.Mudancas, resumo.Sensor, resumo.Imagem, deteccoes.Count(d => d.Confianca < confiancaMinima));
    }

    /// <summary>O km das duas estações da passagem, do cadastro. Sem estações, vale o km que veio na passagem.</summary>
    private async Task<(decimal? Inicio, decimal? Fim)> KmDasEstacoes(NovaPassagemDto nova)
    {
        if (nova.IdEstacaoInicio is null && nova.IdEstacaoFim is null) return (nova.KmInicio, nova.KmFim);
        if (nova.IdEstacaoInicio is not int inicio || nova.IdEstacaoFim is not int fim)
            throw new RegraDeNegocioException("Informe as duas estações da passagem: a de início e a de fim.");
        if (inicio == fim)
            throw new RegraDeNegocioException("A estação de início e a de fim precisam ser diferentes.");
        var kms = await varredura.KmDasEstacoes([inicio, fim]);
        if (!kms.TryGetValue(inicio, out var kmInicio) || !kms.TryGetValue(fim, out var kmFim))
            throw new RegraDeNegocioException("Não achei uma das estações da passagem.");
        return (kmInicio, kmFim);
    }

    /// <summary>Alguma latitude ou longitude fora do mundo? (a latitude vai de −90 a 90, a longitude de −180 a 180).</summary>
    private static bool CoordenadaInvalida(NovaPassagemDto nova)
    {
        static bool Ruim(decimal? latitude, decimal? longitude) => latitude is < -90m or > 90m || longitude is < -180m or > 180m;
        return nova.Pontos.Any(p => Ruim(p.Latitude, p.Longitude)) || nova.Leituras.Any(l => Ruim(l.Latitude, l.Longitude))
            || nova.Deteccoes.Any(d => Ruim(d.Latitude, d.Longitude));
    }

    /// <summary>Calcula o km de cada ponto, leitura e detecção pelo GPS. Onde a conta não dá, fica o km que o carro mandou.</summary>
    private static void CalcularKm(decimal kmInicio, decimal kmFim, List<PontoPassagem> pontos, List<LeituraVia> leituras, List<DeteccaoImagem> deteccoes)
    {
        static PontoGps Ponto(DateTime hora, decimal? latitude, decimal? longitude) => new(hora, (double?)latitude, (double?)longitude);
        // Todo registro com hora entra na conta: os pontos do caminho, as leituras e as detecções, nessa ordem.
        var todos = pontos.Select(p => Ponto(p.Data, p.Latitude, p.Longitude))
            .Concat(leituras.Select(l => Ponto(l.Data, l.Latitude, l.Longitude)))
            .Concat(deteccoes.Select(d => Ponto(d.Data, d.Latitude, d.Longitude)))
            .ToList();
        var km = KmPeloGps.Calcular(kmInicio, kmFim, todos);
        // Devolve o km a cada um, na mesma ordem.
        var i = 0;
        foreach (var p in pontos) p.KmCalculado = km[i++];
        foreach (var l in leituras) l.KmMedido = km[i++] ?? l.KmMedido;
        foreach (var d in deteccoes) d.KmMedido = km[i++] ?? d.KmMedido;
    }

    private async Task<decimal> Numero(string chave)
    {
        var valor = (await parametros.Buscar(chave))?.Valor;
        return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero) && numero >= 0
            ? numero
            : throw new RegraDeNegocioException($"O parâmetro {chave} precisa ser um número.");
    }
}