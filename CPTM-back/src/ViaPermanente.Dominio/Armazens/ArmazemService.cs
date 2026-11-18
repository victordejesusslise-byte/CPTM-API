using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Armazens.Dtos;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Armazens;

/// <summary>
/// Etapa 06, a entrada: só a peça aprovada na verificação entra no armazém, lida com alvo único,
/// numa pilha do tamanho e da classe dela. O lugar de agora fica em DORMENTE.id_pilha, gravado junto com a MOVIMENTACAO.
/// </summary>
public class ArmazemService(IPecaRepository pecas, ICatalogoRepository catalogo, LeituraService leituras, PecaService consulta, IArmazemRepository armazem)
{
    /// <summary>Troca de pilha: a peça no armazém, lida com alvo único, vai para outra pilha do tamanho e da classe dela. Nulo se não existe.</summary>
    public async Task<PecaDto?> TrocarPilha(int idDormente, TrocarPilhaDto dto, int idUsuario)
    {
        var peca = await consulta.BuscarPorId(idDormente);
        if (peca is null)
            return null;
        if (peca.IdPilha is not int pilhaAtual)
            throw new RegraDeNegocioException("A peça não está numa pilha do armazém.");
        var pilha = (await catalogo.ListarPilhas()).FirstOrDefault(p => p.IdPilha == dto.IdPilha && p.Ativo)
            ?? throw new RegraDeNegocioException("Pilha não encontrada.");
        if (pilha.IdPilha == pilhaAtual)
            throw new RegraDeNegocioException("A peça já está nesta pilha.");
        if (pilha.IdTamanho != peca.IdTamanho || pilha.Classe != peca.Classe)
            throw new RegraDeNegocioException($"A pilha {pilha.Codigo} não é de {FormatoHelper.Metros(peca.ComprimentoM)} de {peca.Classe}ª classe.");
        var tag = await pecas.TagVigente(idDormente) ?? throw new RegraDeNegocioException("A peça não tem tag ativa.");
        var leitura = await leituras.ConferirPeca(idDormente, tag, dto.Leitura, idUsuario);
        var agora = DateTime.Now;
        await armazem.TrocarPilha(idDormente, pilha.IdPilha, new Movimentacao
        {
            IdDormente = idDormente, Tipo = TipoMovimentacao.TrocaDePilha, IdPilhaOrigem = pilhaAtual, IdPilhaDestino = pilha.IdPilha,
            IdUsuario = idUsuario, IdLeitor = dto.Leitura.IdLeitor, Data = agora, RegistradoEm = agora, RecebidoEm = agora
        }, leitura);
        return await consulta.BuscarPorId(idDormente);
    }

    /// <summary>Devolve nulo se a peça não existe. Sem pilha escolhida, usa a sugerida no armazém da chegada.</summary>
    public async Task<PecaDto?> Guardar(int idDormente, GuardarPecaDto dto, int idUsuario)
    {
        var peca = await consulta.BuscarPorId(idDormente);
        if (peca is null)
            return null;
        if ((await pecas.Verificacao(idDormente))?.Resultado != ResultadoVerificacao.Aprovada)
            throw new RegraDeNegocioException("Só a peça aprovada na verificação de qualidade entra no armazém.");
        if (peca.IdPilha is not null)
            throw new RegraDeNegocioException($"A peça já está na pilha {peca.Pilha} de {peca.Armazem}.");
        var tag = await pecas.TagVigente(idDormente) ?? throw new RegraDeNegocioException("A peça não tem tag ativa.");

        var pilhas = (await catalogo.ListarPilhas()).Where(p => p.Ativo).ToList();
        Pilha pilha;
        if (dto.IdPilha is int idPilha)
        {
            pilha = pilhas.FirstOrDefault(p => p.IdPilha == idPilha) ?? throw new RegraDeNegocioException("Pilha não encontrada.");
            if (pilha.IdTamanho != peca.IdTamanho || pilha.Classe != peca.Classe)
                throw new RegraDeNegocioException($"A pilha {pilha.Codigo} não é de {FormatoHelper.Metros(peca.ComprimentoM)} de {peca.Classe}ª classe.");
        }
        else
            pilha = pilhas.FirstOrDefault(p => p.IdArmazem == peca.IdArmazem && p.IdTamanho == peca.IdTamanho && p.Classe == peca.Classe)
                ?? throw new RegraDeNegocioException($"{peca.Armazem} não tem pilha de {FormatoHelper.Metros(peca.ComprimentoM)} de {peca.Classe}ª classe.");

        var leitura = await leituras.ConferirPeca(idDormente, tag, dto.Leitura, idUsuario);
        var agora = DateTime.Now;
        await pecas.Guardar(idDormente, pilha.IdPilha, agora, new Movimentacao
        {
            IdDormente = idDormente, Tipo = TipoMovimentacao.Entrada, IdPilhaDestino = pilha.IdPilha, IdUsuario = idUsuario,
            IdLeitor = dto.Leitura.IdLeitor, Data = agora, IdCliente = dto.IdCliente, RegistradoEm = dto.RegistradoEm ?? agora, RecebidoEm = agora
        }, leitura);
        return await consulta.BuscarPorId(idDormente);
    }

    /// <summary>
    /// O lote da tag lida (Victor, 2026-11-17): as peças do lote que estão no pátio, separadas por tamanho e classe, com quantas faltam
    /// guardar, quantas já estão numa pilha e quantas não foram aprovadas. Nulo se a tag não é de nenhuma peça.
    /// </summary>
    public async Task<LoteParaGuardarDto?> LoteDaTag(string tag)
    {
        var lida = await pecas.BuscarTag(tag.Trim());
        if (lida is null)
            return null;
        var resumo = (await pecas.Resumos(idDormente: lida.IdDormente)).Single();
        return await MontarLote(resumo);
    }

    /// <summary>
    /// Guarda o lote de uma vez (Victor, 2026-11-17): cada grupo (tamanho e classe) vai para a pilha escolhida, todas no armazém do lote.
    /// Entram só as aprovadas sem pilha; a tag de cada peça não é lida, só a que achou o lote. Devolve o lote como ficou.
    /// </summary>
    public async Task<LoteParaGuardarDto> GuardarLote(GuardarLoteDto dto, int idUsuario)
    {
        // A tag lida tem que ser de uma peça do sistema; o lote é o dela.
        var lida = await pecas.BuscarTag(dto.Leitura.Tag?.Trim() ?? "")
            ?? throw new RegraDeNegocioException("A tag lida não é de nenhuma peça cadastrada.");
        var resumo = (await pecas.Resumos(idDormente: lida.IdDormente)).Single();
        var doLote = NoPatio(await pecas.Resumos(idLote: resumo.IdLote));

        // Cada pilha escolhida: ativa, do armazém do lote e do tamanho e da classe do grupo.
        var pilhas = (await catalogo.ListarPilhas()).Where(p => p.Ativo).ToDictionary(p => p.IdPilha);
        var tamanhos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho, t => t.ComprimentoM);
        var agora = DateTime.Now;
        var registradoEm = dto.RegistradoEm ?? agora;
        var entradas = new List<Movimentacao>();
        foreach (var grupo in dto.Grupos)
        {
            var nome = $"{FormatoHelper.Metros(tamanhos.GetValueOrDefault(grupo.IdTamanho))} de {grupo.Classe}ª classe";
            if (!pilhas.TryGetValue(grupo.IdPilha, out var pilha))
                throw new RegraDeNegocioException($"Pilha não encontrada para {nome}.");
            if (pilha.IdArmazem != dto.IdArmazem)
                throw new RegraDeNegocioException($"A pilha {pilha.Codigo} não é do armazém escolhido para o lote.");
            if (pilha.IdTamanho != grupo.IdTamanho || pilha.Classe != grupo.Classe)
                throw new RegraDeNegocioException($"A pilha {pilha.Codigo} não é de {nome}.");
            // As peças do grupo que entram: aprovadas e ainda sem pilha.
            entradas.AddRange(doLote.Where(p => p.IdTamanho == grupo.IdTamanho && p.Classe == grupo.Classe && AGuardar(p))
                .Select(p => new Movimentacao
                {
                    IdDormente = p.IdDormente, Tipo = TipoMovimentacao.Entrada, IdPilhaDestino = pilha.IdPilha, IdUsuario = idUsuario,
                    IdLeitor = p.IdDormente == lida.IdDormente ? dto.Leitura.IdLeitor : null, Data = agora, RegistradoEm = registradoEm, RecebidoEm = agora
                }));
        }
        if (entradas.Count == 0)
            throw new RegraDeNegocioException("Nenhuma peça aprovada deste lote está esperando pilha.");

        // A leitura da tag que achou o lote fica registrada na peça lida, conferida como nas outras entradas.
        var tagLida = await pecas.TagVigente(lida.IdDormente) ?? throw new RegraDeNegocioException("A peça lida não tem tag ativa.");
        var leitura = await leituras.ConferirPeca(lida.IdDormente, tagLida, dto.Leitura, idUsuario);
        await pecas.GuardarVarias(entradas, agora, leitura);
        return await MontarLote((await pecas.Resumos(idDormente: lida.IdDormente)).Single());
    }

    /// <summary>Monta o lote da peça: só as peças no pátio, agrupadas por tamanho e classe (o maior grupo primeiro).</summary>
    private async Task<LoteParaGuardarDto> MontarLote(PecaResumo lida)
    {
        var doLote = NoPatio(await pecas.Resumos(idLote: lida.IdLote));
        var tamanhos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho, t => t.ComprimentoM);
        var lote = await pecas.LoteDoPedido(lida.IdPedido);
        var grupos = doLote.GroupBy(p => (p.IdTamanho, p.Classe))
            .Select(g => new GrupoDoLoteDto(g.Key.IdTamanho, tamanhos.GetValueOrDefault(g.Key.IdTamanho), g.Key.Classe,
                g.Count(AGuardar), g.Count(p => p.IdPilha is not null), g.Count(p => p.Verificacao != ResultadoVerificacao.Aprovada)))
            .OrderByDescending(g => g.AGuardar).ThenBy(g => g.IdTamanho).ThenBy(g => g.Classe)
            .ToList();
        return new LoteParaGuardarDto(lida.IdLote, lote?.Codigo ?? $"L{lida.IdLote:00}", lida.IdDormente, lida.Codigo, lida.IdArmazemChegada, doLote.Count, grupos);
    }

    /// <summary>As peças do lote que estão no pátio: nem na via, nem retiradas dela.</summary>
    private static List<PecaResumo> NoPatio(IEnumerable<PecaResumo> pecasDoLote)
        => pecasDoLote.Where(p => p.IdTrecho is null && p.RetiradaEm is null).ToList();

    /// <summary>A peça que o botão guarda: aprovada na verificação e ainda sem pilha.</summary>
    private static bool AGuardar(PecaResumo p) => p.Verificacao == ResultadoVerificacao.Aprovada && p.IdPilha is null;
}