using System.Globalization;
using ViaPermanente.Dominio.Dtos.Varredura;
using ViaPermanente.Dominio.Models.Campo;
using ViaPermanente.Dominio.Models.Via;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dominio.Services;

/// <summary>
/// Etapa 08: a passagem noturna do carro. Grava as leituras (LEITURA_VIA, 90 dias) e as detecções da câmera,
/// abre os alertas (sensor: tag muda, rachadura, posição; imagem: acima da confiança mínima), atualiza a posição atual
/// de cada peça e grava um evento quando ela muda de faixa. A máquina aponta, o técnico decide.
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

        var limite = await Numero(ChaveLimite);
        var atencao = await Numero(ChaveAtencao);
        var confiancaMinima = await Numero(ChaveConfianca);
        var tags = nova.Leituras.Select(l => l.Tag).Concat(nova.Deteccoes.Select(d => d.Tag))
            .Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!.Trim()).Distinct().ToList();
        var donos = await varredura.DonosDasTags(tags);
        int? Dono(string? tag, int? idDormente) => idDormente ?? (tag is not null && donos.TryGetValue(tag.Trim(), out var id) ? id : null);

        var passagem = new Passagem
        {
            Codigo = codigo, Veiculo = nova.Veiculo, DataInicio = nova.DataInicio, DataFim = nova.DataFim, KmInicio = nova.KmInicio, KmFim = nova.KmFim
        };
        var leituras = nova.Leituras.Select(l => new LeituraVia
        {
            IdDormente = Dono(l.Tag, l.IdDormente), Leitura = l.Leitura, KmMedido = l.KmMedido, DesvioMm = l.DesvioMm,
            Data = l.Data == default ? nova.DataInicio : l.Data, Umidade = l.Umidade, Temperatura = l.Temperatura, RachaduraSuspeita = l.RachaduraSuspeita
        }).ToList();
        var deteccoes = nova.Deteccoes.Select(d => new DeteccaoImagem
        {
            IdDormente = Dono(d.Tag, null), KmMedido = d.KmMedido, QuadroArquivo = d.QuadroArquivo.Trim(), Camera = d.Camera,
            CaixaX = d.CaixaX, CaixaY = d.CaixaY, CaixaLargura = d.CaixaLargura, CaixaAltura = d.CaixaAltura,
            Defeito = d.Defeito, Grau = d.Grau, Confianca = d.Confianca, VersaoModelo = d.VersaoModelo, Data = d.Data == default ? nova.DataInicio : d.Data
        }).ToList();

        var resumo = (Sensor: 0, Imagem: 0, Mudancas: 0);
        await varredura.EmTransacao(async () =>
        {
            await varredura.GravarPassagem(passagem, leituras, deteccoes);
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

    private async Task<decimal> Numero(string chave)
    {
        var valor = (await parametros.Buscar(chave))?.Valor;
        return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero) && numero >= 0
            ? numero
            : throw new RegraDeNegocioException($"O parâmetro {chave} precisa ser um número.");
    }
}