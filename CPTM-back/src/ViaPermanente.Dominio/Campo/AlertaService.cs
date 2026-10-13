using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// Etapa 09: a fila de alertas, em duas abas (imagem e sensores). O operador da via e o técnico vão ao local e registram
/// o que viram, com foto (a vistoria não fecha o alerta). Só o técnico decide: "Confere" abre a inspeção; "Não confere" descarta, com motivo.
/// </summary>
public class AlertaService(IAlertaRepository alertas, IPecaRepository pecas, LeituraService leituras, PecaService consulta)
{
    public async Task<IReadOnlyList<AlertaDto>> Listar(OrigemAlerta? origem, StatusAlerta? status) => await Montar(await alertas.Listar(origem, status));

    public async Task<AlertaDto?> BuscarPorId(int idAlerta) => (await Montar(await alertas.Listar(null, null, idAlerta))).FirstOrDefault();

    /// <summary>Devolve nulo se o alerta não existe.</summary>
    public async Task<AlertaDto?> Vistoriar(int idAlerta, NovaVistoriaDto dto, int idUsuario)
    {
        var alerta = (await alertas.Listar(null, null, idAlerta)).FirstOrDefault();
        if (alerta is null)
            return null;
        if (alerta.Status != StatusAlerta.Aberto)
            throw new RegraDeNegocioException("O alerta já foi decidido pelo técnico.");
        if (string.IsNullOrWhiteSpace(dto.FotoArquivo))
            throw new RegraDeNegocioException("A vistoria precisa de foto (Foto em todo registro de campo).");

        var agora = DateTime.Now;
        LeituraDormente? leitura = null;
        int? pecaIdentificada = null;
        string? tagLida = null;
        if (dto.TagNaoResponde)
        {
            if (alerta.IdDormente is null)
                throw new RegraDeNegocioException("O alerta não tem peça: sem a tag, o registro precisa de outra vistoria que leia a tag.");
            leitura = new LeituraDormente
            {
                IdDormente = alerta.IdDormente, IdLeitor = dto.Leitura?.IdLeitor, IdUsuario = idUsuario, MetodoCaptura = MetodoCaptura.ModuloUhf,
                Leitura = false, DataLeitura = agora, IdModo = dto.Leitura?.IdModo, RegistradoEm = agora, RecebidoEm = agora,
                Observacao = "Tag não respondeu na vistoria do alerta"
            };
        }
        else
        {
            if (dto.Leitura is null)
                throw new RegraDeNegocioException("Leia a tag da peça com o módulo, ou marque que ela não responde.");
            if (alerta.IdDormente is int idDormente)
            {
                var tag = await pecas.TagAtiva(idDormente) ?? throw new RegraDeNegocioException("A peça do alerta não tem tag ativa.");
                leitura = await leituras.ConferirPeca(idDormente, tag, dto.Leitura, idUsuario); // a da vizinha é recusada
            }
            else
            {
                // Alerta sem peça: a tag lida com alvo único dá a peça ao alerta.
                tagLida = dto.Leitura.Tag.Trim();
                var dona = await pecas.BuscarTag(tagLida);
                if (dona is null || !dona.Ativo)
                    throw new RegraDeNegocioException($"A tag {tagLida} não está associada a nenhuma peça.");
                leitura = await leituras.ConferirPeca(dona.IdDormente, dona, dto.Leitura, idUsuario);
                pecaIdentificada = dona.IdDormente;
            }
        }

        await alertas.Vistoriar(new VistoriaAlerta
        {
            IdAlerta = idAlerta, IdUsuario = idUsuario, Data = agora, Viu = dto.Viu, TagLida = tagLida,
            FotoArquivo = dto.FotoArquivo.Trim(), Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? null : dto.Observacao.Trim(),
            IdCliente = dto.IdCliente, RegistradoEm = dto.RegistradoEm ?? agora, RecebidoEm = agora
        }, leitura, pecaIdentificada);
        return await BuscarPorId(idAlerta);
    }

    /// <summary>"Não confere": só o técnico, com motivo. Devolve nulo se o alerta não existe.</summary>
    public async Task<AlertaDto?> Descartar(int idAlerta, string motivo, int idUsuario)
    {
        var alerta = (await alertas.Listar(null, null, idAlerta)).FirstOrDefault();
        if (alerta is null)
            return null;
        if (string.IsNullOrWhiteSpace(motivo))
            throw new RegraDeNegocioException("Informe o motivo do descarte.");
        if (alerta.Status != StatusAlerta.Aberto)
            throw new RegraDeNegocioException("O alerta já foi decidido.");
        await alertas.Descartar(idAlerta, idUsuario, DateTime.Now, motivo.Trim());
        return await BuscarPorId(idAlerta);
    }

    private async Task<IReadOnlyList<AlertaDto>> Montar(IReadOnlyList<Alerta> lista)
    {
        if (lista.Count == 0)
            return [];
        var deteccoes = await alertas.Deteccoes(lista.Where(a => a.IdDeteccao != null).Select(a => a.IdDeteccao!.Value).ToList());
        var kms = await alertas.KmDasLeituras(lista.Where(a => a.IdLeituraVia != null).Select(a => a.IdLeituraVia!.Value).ToList());
        var vistorias = await alertas.ContarVistorias(lista.Select(a => a.IdAlerta).ToList());
        var porPeca = await consulta.PorIds(lista.Where(a => a.IdDormente != null).Select(a => a.IdDormente!.Value).Distinct().ToList());

        return lista.Select(a =>
        {
            var d = a.IdDeteccao is int idD ? deteccoes.GetValueOrDefault(idD) : null;
            var peca = a.IdDormente is int idP ? porPeca.GetValueOrDefault(idP) : null;
            var km = d?.KmMedido ?? (a.IdLeituraVia is long idL ? kms.GetValueOrDefault(idL) : null);
            return new AlertaDto(a.IdAlerta, a.Origem.ToString(), a.Tipo.ToString(), a.Status.ToString(), a.Data, a.DesvioMm, km,
                a.IdDormente, peca?.Codigo, peca?.Tag,
                d is null ? null : new DeteccaoDto(d.IdDeteccao, d.QuadroArquivo, d.Camera, d.CaixaX, d.CaixaY, d.CaixaLargura, d.CaixaAltura,
                    d.Defeito.ToString(), d.Grau, d.Confianca, d.VersaoModelo),
                vistorias.GetValueOrDefault(a.IdAlerta), a.IdUsuarioDecisao, a.DataDecisao, a.MotivoDescarte);
        }).ToList();
    }
}