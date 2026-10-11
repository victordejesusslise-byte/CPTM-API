using ViaPermanente.Dominio.Dtos.Armazem;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dominio.Services;

/// <summary>
/// A conferência do armazém com o leitor, pilha a pilha, no modo Armazém: o que devia estar, o que respondeu, o que falta
/// e o que sobra (peça lida numa pilha que não é a dela). Só fecha com todas as pilhas lidas; conferência parcial não conta.
/// Os números ficam guardados ao concluir, porque o estoque muda depois.
/// </summary>
public class ConferenciaService(IArmazemRepository armazem, ICatalogoRepository catalogo)
{
    /// <summary>Abre a conferência do armazém (uma aberta por vez). Nulo se o armazém não existe.</summary>
    public async Task<ConferenciaDto?> Iniciar(int idArmazem, int? idLeitor, int idUsuario)
    {
        if ((await catalogo.ListarArmazens()).All(a => a.IdArmazem != idArmazem || !a.Ativo))
            return null;
        if (await armazem.ConferenciaAberta(idArmazem) is { } aberta)
            return await Montar(aberta);
        var conferencia = new ConferenciaArmazem { IdArmazem = idArmazem, IdUsuario = idUsuario, IdLeitor = idLeitor, IniciadaEm = DateTime.Now, RegistradoEm = DateTime.Now, RecebidoEm = DateTime.Now };
        await armazem.CriarConferencia(conferencia);
        return await Montar(conferencia);
    }

    /// <summary>Grava a leitura de uma pilha: uma linha por tag que respondeu, com a pilha. Ler de novo a pilha soma às anteriores.</summary>
    public async Task<ConferenciaDto?> LerPilha(int idConferencia, LerPilhaDto dto, int idUsuario)
    {
        var conferencia = await armazem.BuscarConferencia(idConferencia);
        if (conferencia is null)
            return null;
        if (conferencia.ConcluidaEm is not null)
            throw new RegraDeNegocioException("A conferência já foi concluída.");
        if ((await catalogo.ListarPilhas()).All(p => p.IdPilha != dto.IdPilha || p.IdArmazem != conferencia.IdArmazem))
            throw new RegraDeNegocioException("A pilha não é deste armazém.");
        var tags = dto.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct().ToList();
        var donos = await armazem.DonosDasTags(tags);
        var agora = DateTime.Now;
        var leituras = tags.Select(t => new LeituraDormente
        {
            IdDormente = donos.TryGetValue(t, out var id) ? id : null, IdLeitor = dto.IdLeitor, IdUsuario = idUsuario,
            MetodoCaptura = MetodoCaptura.ModuloUhf, Leitura = true, DataLeitura = agora, TagLida = t, IdModo = dto.IdModo,
            TagsVistas = tags.Count, IdConferencia = idConferencia, IdPilha = dto.IdPilha, RegistradoEm = agora, RecebidoEm = agora
        }).ToList();
        if (leituras.Count == 0) // a pilha foi lida e nada respondeu: fica uma leitura sem tag, para contar a pilha como lida
            leituras.Add(new LeituraDormente
            {
                IdUsuario = idUsuario, MetodoCaptura = MetodoCaptura.ModuloUhf, Leitura = false, DataLeitura = agora, IdModo = dto.IdModo,
                IdLeitor = dto.IdLeitor, TagsVistas = 0, IdConferencia = idConferencia, IdPilha = dto.IdPilha, RegistradoEm = agora, RecebidoEm = agora,
                Observacao = "Nenhuma tag respondeu na pilha"
            });
        await armazem.RegistrarLeituras(leituras);
        return await Montar(conferencia);
    }

    /// <summary>Conclui e guarda os números. Recusa enquanto faltar pilha por ler.</summary>
    public async Task<ConferenciaDto?> Concluir(int idConferencia)
    {
        var conferencia = await armazem.BuscarConferencia(idConferencia);
        if (conferencia is null)
            return null;
        if (conferencia.ConcluidaEm is not null)
            throw new RegraDeNegocioException("A conferência já foi concluída.");
        var situacao = await Montar(conferencia);
        if (situacao.PilhasFaltando.Count > 0)
            throw new RegraDeNegocioException($"Faltam pilhas por ler: {string.Join(", ", situacao.PilhasFaltando)}. Conferência parcial não conta.");
        conferencia.ConcluidaEm = DateTime.Now;
        conferencia.Esperadas = situacao.Esperadas;
        conferencia.Lidas = situacao.Lidas;
        conferencia.Faltando = situacao.Faltando.Count;
        conferencia.AMais = situacao.AMais.Count;
        await armazem.ConcluirConferencia(conferencia);
        return await Montar(conferencia);
    }

    public async Task<ConferenciaDto?> BuscarPorId(int idConferencia)
        => await armazem.BuscarConferencia(idConferencia) is { } c ? await Montar(c) : null;

    private async Task<ConferenciaDto> Montar(ConferenciaArmazem c)
    {
        var pilhas = (await catalogo.ListarPilhas()).Where(p => p.IdArmazem == c.IdArmazem && p.Ativo).ToDictionary(p => p.IdPilha);
        var nome = (await catalogo.ListarArmazens()).First(a => a.IdArmazem == c.IdArmazem).Nome;
        var leituras = await armazem.LeiturasDaConferencia(c.IdConferencia);
        var esperadas = await armazem.PecasNoArmazem(c.IdArmazem);
        var lidas = leituras.Where(l => l.IdDormente is not null).GroupBy(l => l.IdDormente!.Value).ToDictionary(g => g.Key, g => g.Last().IdPilha);
        var tagDe = esperadas.ToDictionary(e => e.IdDormente, e => e.Tag);
        string? Codigo(int? idPilha) => idPilha is int id && pilhas.TryGetValue(id, out var p) ? p.Codigo : null;

        var pilhasLidas = leituras.Select(l => l.IdPilha).OfType<int>().Distinct().ToHashSet();
        var faltando = esperadas.Where(e => !lidas.ContainsKey(e.IdDormente))
            .Select(e => new PecaConferidaDto(e.IdDormente, e.Tag, Codigo(e.IdPilha), null)).ToList();
        var esperadaEm = esperadas.ToDictionary(e => e.IdDormente, e => (int?)e.IdPilha);
        var aMais = lidas.Where(l => esperadaEm.GetValueOrDefault(l.Key) != l.Value)
            .Select(l => new PecaConferidaDto(l.Key, tagDe.GetValueOrDefault(l.Key) ?? leituras.Last(x => x.IdDormente == l.Key).TagLida,
                Codigo(esperadaEm.GetValueOrDefault(l.Key)), Codigo(l.Value))).ToList();

        return new ConferenciaDto(c.IdConferencia, c.IdArmazem, nome, c.IniciadaEm, c.ConcluidaEm,
            pilhas.Values.Where(p => pilhasLidas.Contains(p.IdPilha)).Select(p => p.Codigo).OrderBy(x => x).ToList(),
            pilhas.Values.Where(p => !pilhasLidas.Contains(p.IdPilha)).Select(p => p.Codigo).OrderBy(x => x).ToList(),
            c.Esperadas ?? esperadas.Count, c.Lidas ?? lidas.Count, faltando, aMais,
            leituras.Where(l => l.IdDormente is null && l.TagLida is not null).Select(l => l.TagLida!).Distinct().ToList());
    }
}