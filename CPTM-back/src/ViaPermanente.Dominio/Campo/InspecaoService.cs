using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// Etapa 10: o checklist da inspeção, feito pelo técnico com a peça lida e foto obrigatória (Confirmar alerta é fazer o checklist).
/// A inspeção fecha os alertas abertos da peça, das duas origens: confirmados se reprovou, descartados se aprovou.
/// Data, início, fim, duração e foto nunca mudam.
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
        if (inspecao.IdUsuario != idUsuario)
            throw new RegraDeNegocioException("Só quem fez a inspeção pode corrigir.");
        if (string.IsNullOrWhiteSpace(dto.Motivo))
            throw new RegraDeNegocioException("A correção precisa de motivo.");

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

        var cancelamento = resultadoAntes == ResultadoInspecao.Reprovada && inspecao.Resultado == ResultadoInspecao.Aprovada
            ? await trocaService.Cancelamento(idInspecao, idUsuario, $"Inspeção {idInspecao} corrigida: a peça foi aprovada.")
            : null;
        var correcao = new InspecaoCorrecao
        {
            IdInspecao = idInspecao, IdUsuario = idUsuario, Data = DateTime.Now, RetratoAntes = antes, RetratoDepois = Retrato(inspecao),
            Motivo = dto.Motivo.Trim(), IdTrocaCancelada = cancelamento?.TrocaAlterada?.IdTroca
        };
        await trocas.Gravar(new MudancasDaTroca { TrocaAlterada = cancelamento?.TrocaAlterada, InspecaoAlterada = inspecao, Correcao = correcao }
            .Com(cancelamento ?? new MudancasDaTroca()));
        return ParaDto(inspecao);
    }

    private static string Retrato(Inspecao i) => System.Text.Json.JsonSerializer.Serialize(new
    {
        EstadoConservacao = i.EstadoConservacao.ToString(), Rachadura = i.Rachadura.ToString(), AtaqueBiologico = i.AtaqueBiologico.ToString(),
        i.UmidadePct, i.VidaUtilMeses, i.TipoMadeiraConfere, i.Observacao, Resultado = i.Resultado.ToString()
    });

    public async Task<InspecaoDto> Registrar(NovaInspecaoDto dto, int idUsuario)
    {
        if (await pecas.Buscar(dto.IdDormente) is null)
            throw new RegraDeNegocioException("Peça não encontrada.");
        if (string.IsNullOrWhiteSpace(dto.FotoArquivo))
            throw new RegraDeNegocioException("A inspeção precisa de foto (Foto em todo registro de campo).");
        if (dto.FinalizadoEm < dto.IniciadoEm || dto.IniciadoEm == default)
            throw new RegraDeNegocioException("Informe o início e o fim da inspeção (o fim depois do início).");
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
        if (dto.IdInspecaoOrigem is int idOrigem && (await inspecoes.Listar(idInspecao: idOrigem)).FirstOrDefault()?.IdDormente != dto.IdDormente)
            throw new RegraDeNegocioException("A inspeção de origem do retrabalho não é desta peça.");

        var tag = await pecas.TagAtiva(dto.IdDormente) ?? throw new RegraDeNegocioException("A peça não tem tag ativa.");
        var leitura = await leituras.ConferirPeca(dto.IdDormente, tag, dto.Leitura, idUsuario);

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
            IdCliente = dto.IdCliente, RegistradoEm = dto.RegistradoEm ?? agora, RecebidoEm = agora
        };
        await inspecoes.Registrar(inspecao, leitura,
            resultado == ResultadoInspecao.Reprovada ? StatusAlerta.Confirmado : StatusAlerta.Descartado,
            resultado == ResultadoInspecao.Aprovada ? "A inspeção aprovou a peça." : null);
        return ParaDto(inspecao);
    }

    public async Task<IReadOnlyList<InspecaoDto>> DaPeca(int idDormente) => (await inspecoes.Listar(idDormente: idDormente)).Select(ParaDto).ToList();

    public async Task<InspecaoDto?> BuscarPorId(int idInspecao) => (await inspecoes.Listar(idInspecao: idInspecao)).Select(ParaDto).FirstOrDefault();

    internal static InspecaoDto ParaDto(Inspecao i)
        => new(i.IdInspecao, i.IdDormente, i.IdUsuario, i.IdAlerta, i.IdInspecaoOrigem, i.IniciadoEm, i.FinalizadoEm, i.DuracaoS, i.VersaoChecklist,
            i.EstadoConservacao.ToString(), i.Rachadura.ToString(), i.AtaqueBiologico.ToString(), i.UmidadePct, i.VidaUtilMeses, i.TipoMadeiraConfere,
            i.Resultado.ToString(), ChecklistInspecao.MotivosDeReprovacao(i.EstadoConservacao, i.Rachadura, i.AtaqueBiologico, i.VidaUtilMeses),
            i.Observacao, i.FotoArquivo);
}