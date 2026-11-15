using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// Etapa 10: o técnico faz o checklist da peça, com a tag lida e foto obrigatória (Confirmar alerta é fazer o checklist).
/// A inspeção fecha os alertas abertos da peça: confirmados se reprovou, descartados se aprovou. Data, horários e foto nunca mudam.
/// </summary>
public class InspecaoService(IInspecaoRepository inspecoes, IAlertaRepository alertas, IPecaRepository pecas, LeituraService leituras,
    ITrocaRepository trocas, TrocaService trocaService)
{
    /// <summary>
    /// Corrigir não é refazer: só quem fez, com motivo. Guarda o antes e o depois em INSPECAO_CORRECAO e muda só as respostas.
    /// Se a correção passa a aprovar a peça, a troca aberta pela inspeção é cancelada (e a peça separada volta ao armazém).
    /// Devolve nulo se a inspeção não existe.
    /// </summary>
    public async Task<InspecaoDto?> Corrigir(int idInspecao, CorrigirInspecaoDto dto, int idUsuario)
    {
        var inspecao = (await inspecoes.Listar(idInspecao: idInspecao)).FirstOrDefault();
        if (inspecao is null)
            return null;
        // Só o próprio técnico corrige, e sempre com motivo.
        if (inspecao.IdUsuario != idUsuario)
            throw new RegraDeNegocioException("Só quem fez a inspeção pode corrigir.");
        if (string.IsNullOrWhiteSpace(dto.Motivo))
            throw new RegraDeNegocioException("A correção precisa de motivo.");

        // Guarda o retrato de antes e troca só as respostas do checklist.
        var antes = Retrato(inspecao);
        var resultadoAntes = inspecao.Resultado;
        inspecao.EstadoConservacao = dto.EstadoConservacao;
        inspecao.Rachadura = dto.Rachadura;
        inspecao.AtaqueBiologico = dto.AtaqueBiologico;
        inspecao.UmidadePct = dto.UmidadePct;
        inspecao.VidaUtilMeses = dto.VidaUtilMeses;
        inspecao.TipoMadeiraConfere = dto.TipoMadeiraConfere;
        inspecao.Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? inspecao.Observacao : dto.Observacao.Trim();
        inspecao.Resultado = ChecklistInspecao.Resultado(dto.EstadoConservacao, dto.Rachadura, dto.AtaqueBiologico, dto.VidaUtilMeses);

        // Reprovada que virou aprovada: a troca pedida por esta inspeção é cancelada.
        var cancelamento = resultadoAntes == ResultadoInspecao.Reprovada && inspecao.Resultado == ResultadoInspecao.Aprovada
            ? await trocaService.Cancelamento(idInspecao, idUsuario, $"Inspeção {idInspecao} corrigida: a peça foi aprovada.")
            : null;
        var correcao = new InspecaoCorrecao
        {
            IdInspecao = idInspecao, IdUsuario = idUsuario, Data = DateTime.Now, RetratoAntes = antes, RetratoDepois = Retrato(inspecao),
            Motivo = dto.Motivo.Trim(), IdTrocaCancelada = cancelamento?.TrocaAlterada?.IdTroca
        };
        // Grava a inspeção, a correção e o cancelamento da troca juntos.
        await trocas.Gravar(new MudancasDaTroca { TrocaAlterada = cancelamento?.TrocaAlterada, InspecaoAlterada = inspecao, Correcao = correcao }
            .Com(cancelamento ?? new MudancasDaTroca()));
        return ParaDto(inspecao);
    }

    /// <summary>As respostas da inspeção em JSON, para o antes e o depois da correção.</summary>
    private static string Retrato(Inspecao i) => System.Text.Json.JsonSerializer.Serialize(new
    {
        EstadoConservacao = i.EstadoConservacao.ToString(), Rachadura = i.Rachadura.ToString(), AtaqueBiologico = i.AtaqueBiologico.ToString(),
        i.UmidadePct, i.VidaUtilMeses, i.TipoMadeiraConfere, i.Observacao, Resultado = i.Resultado.ToString()
    });

    /// <summary>Grava uma inspeção nova e fecha os alertas abertos da peça.</summary>
    public async Task<InspecaoDto> Registrar(NovaInspecaoDto dto, int idUsuario)
    {
        // O celular mandou de novo (a rede caiu antes da resposta): devolve a que já entrou.
        if (dto.IdCliente is Guid idCliente && await inspecoes.IdPorCliente(idCliente) is int ja)
            return (await BuscarPorId(ja))!;
        // A peça existe, a foto veio e o horário faz sentido.
        if (await pecas.Buscar(dto.IdDormente) is null)
            throw new RegraDeNegocioException("Peça não encontrada.");
        if (string.IsNullOrWhiteSpace(dto.FotoArquivo))
            throw new RegraDeNegocioException("A inspeção precisa de foto (Foto em todo registro de campo).");
        if (dto.FinalizadoEm < dto.IniciadoEm || dto.IniciadoEm == default)
            throw new RegraDeNegocioException("Informe o início e o fim da inspeção (o fim depois do início).");
        // Inspeção aberta por um alerta: o alerta tem de ser desta peça e ainda estar aberto.
        if (dto.IdAlerta is int idAlerta)
        {
            var alerta = (await alertas.Listar(null, null, idAlerta)).FirstOrDefault()
                ?? throw new RegraDeNegocioException("Alerta não encontrado.");
            if (alerta.IdDormente is null)
                throw new RegraDeNegocioException("O alerta ainda não tem peça: leia a tag no local antes de abrir o checklist.");
            if (alerta.IdDormente != dto.IdDormente)
                throw new RegraDeNegocioException("O alerta é de outra peça.");
            if (alerta.Status != StatusAlerta.Aberto)
                throw new RegraDeNegocioException("O alerta já foi decidido.");
        }
        // Retrabalho: a inspeção de origem tem de ser da mesma peça.
        if (dto.IdInspecaoOrigem is int idOrigem && (await inspecoes.Listar(idInspecao: idOrigem)).FirstOrDefault()?.IdDormente != dto.IdDormente)
            throw new RegraDeNegocioException("A inspeção de origem do retrabalho não é desta peça.");

        // A tag lida tem de ser a desta peça (alvo único).
        var tag = await pecas.TagVigente(dto.IdDormente) ?? throw new RegraDeNegocioException("A peça não tem tag ativa.");
        var leitura = await leituras.ConferirPeca(dto.IdDormente, tag, dto.Leitura, idUsuario);

        // O resultado sai das respostas; a duração, do início e do fim.
        var resultado = ChecklistInspecao.Resultado(dto.EstadoConservacao, dto.Rachadura, dto.AtaqueBiologico, dto.VidaUtilMeses);
        var agora = DateTime.Now;
        var inspecao = new Inspecao
        {
            IdDormente = dto.IdDormente, IdUsuario = idUsuario, IdAlerta = dto.IdAlerta, IdInspecaoOrigem = dto.IdInspecaoOrigem,
            IniciadoEm = dto.IniciadoEm, FinalizadoEm = dto.FinalizadoEm, DuracaoS = (int)(dto.FinalizadoEm - dto.IniciadoEm).TotalSeconds,
            VersaoChecklist = ChecklistInspecao.VersaoAtual, EstadoConservacao = dto.EstadoConservacao, Rachadura = dto.Rachadura,
            AtaqueBiologico = dto.AtaqueBiologico, UmidadePct = dto.UmidadePct, VidaUtilMeses = dto.VidaUtilMeses,
            TipoMadeiraConfere = dto.TipoMadeiraConfere, Resultado = resultado,
            Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? null : dto.Observacao.Trim(), FotoArquivo = dto.FotoArquivo.Trim(),
            IdCliente = dto.IdCliente, RegistradoEm = HoraDoCampo.De(dto.RegistradoEm, agora), RecebidoEm = agora
        };
        // Reprovou: os alertas abertos da peça viram confirmados. Aprovou: viram descartados.
        await inspecoes.Registrar(inspecao, leitura,
            resultado == ResultadoInspecao.Reprovada ? StatusAlerta.Confirmado : StatusAlerta.Descartado,
            resultado == ResultadoInspecao.Aprovada ? "A inspeção aprovou a peça." : null);
        return ParaDto(inspecao);
    }

    /// <summary>As inspeções de uma peça.</summary>
    public async Task<IReadOnlyList<InspecaoDto>> DaPeca(int idDormente) => (await inspecoes.Listar(idDormente: idDormente)).Select(ParaDto).ToList();

    /// <summary>Uma inspeção pelo id; nula se não existe.</summary>
    public async Task<InspecaoDto?> BuscarPorId(int idInspecao) => (await inspecoes.Listar(idInspecao: idInspecao)).Select(ParaDto).FirstOrDefault();

    /// <summary>A inspeção como a tela mostra, com os motivos da reprovação.</summary>
    internal static InspecaoDto ParaDto(Inspecao i)
        => new(i.IdInspecao, i.IdDormente, i.IdUsuario, i.IdAlerta, i.IdInspecaoOrigem, i.IniciadoEm, i.FinalizadoEm, i.DuracaoS, i.VersaoChecklist,
            i.EstadoConservacao.ToString(), i.Rachadura.ToString(), i.AtaqueBiologico.ToString(), i.UmidadePct, i.VidaUtilMeses, i.TipoMadeiraConfere,
            i.Resultado.ToString(), ChecklistInspecao.MotivosDeReprovacao(i.EstadoConservacao, i.Rachadura, i.AtaqueBiologico, i.VidaUtilMeses),
            i.Observacao, i.FotoArquivo);
}