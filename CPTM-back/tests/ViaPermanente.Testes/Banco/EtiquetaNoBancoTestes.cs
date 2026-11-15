using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Armazens;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>
/// Etiqueta na GRAVACAO_TAG, sem a RFID (Victor, 2026-11-14): a GRAVACAO_TAG é o histórico da etiqueta (Gravada, Ligada, Desligada), e a
/// marca vigente guarda no banco as regras "um BR vigente é de uma peça só" e "uma etiqueta vigente por peça". Peças do exemplo da apresentação.
/// </summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class EtiquetaNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    private int usuario;

    public async Task InitializeAsync()
    {
        await BancoDeTeste.Recriar(db);
        usuario = (await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto { Nome = "S", Email = "s@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor })).Id;
        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(usuario);
        db.ChangeTracker.Clear();
    }

    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    private static string Tag(int n) => $"BR{n:D10}";
    private async Task<int> Peca(int n) => (await db.Set<GravacaoTag>().AsNoTracking().SingleAsync(g => g.TagGravada == Tag(n) && g.Vigente)).IdDormente!.Value;

    private GravacaoTag Linha(TipoGravacao tipo, int idDormente, string tag, bool vigente = true,
        ResultadoGravacao resultado = ResultadoGravacao.Ok) => new()
    {
        Tipo = tipo, IdDormente = idDormente, TagGravada = tag, Resultado = resultado, TagsVistas = 1, Vigente = vigente,
        IdUsuario = usuario, Data = DateTime.Now, RegistradoEm = DateTime.Now
    };

    /// <summary>Grava a linha direto no banco; devolve se o banco aceitou.</summary>
    private async Task<bool> Aceita(GravacaoTag linha)
    {
        db.ChangeTracker.Clear();
        db.Add(linha);
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task O_banco_guarda_as_regras_da_etiqueta_vigente()
    {
        var (peca1, peca2) = (await Peca(1), await Peca(2));

        // Uma etiqueta vigente por peça: a peça 1 já tem a BR…01.
        Assert.False(await Aceita(Linha(TipoGravacao.Ligada, peca1, "BR9000000001")));
        // Um BR vigente é de uma peça só: a peça 2 perde a etiqueta dela e tenta ficar com a BR…01.
        await db.Set<GravacaoTag>().Where(g => g.IdDormente == peca2 && g.Vigente).ExecuteUpdateAsync(s => s.SetProperty(g => g.Vigente, false));
        Assert.False(await Aceita(Linha(TipoGravacao.Ligada, peca2, Tag(1))));
        // Só a Gravada ok ou a Ligada podem ser vigentes.
        Assert.False(await Aceita(Linha(TipoGravacao.Desligada, peca2, "BR9000000002")));
        Assert.False(await Aceita(Linha(TipoGravacao.Gravada, peca2, "BR9000000002", resultado: ResultadoGravacao.Divergente)));
        // A vigente tem peça.
        Assert.False(await Aceita(new GravacaoTag
        {
            Tipo = TipoGravacao.Gravada, TagGravada = "BR9000000002", Resultado = ResultadoGravacao.Ok, TagsVistas = 1, Vigente = true,
            IdUsuario = usuario, Data = DateTime.Now, RegistradoEm = DateTime.Now
        }));

        Assert.True(await Aceita(Linha(TipoGravacao.Ligada, peca2, "BR9000000002")));
        Assert.True(await Aceita(Linha(TipoGravacao.Desligada, peca2, Tag(2), vigente: false)));
        Assert.Equal("BR9000000002", (await BancoDeTeste.Pecas(db).BuscarPorId(peca2))!.Tag);
    }

    [Fact]
    public async Task O_exemplo_grava_a_etiqueta_e_desliga_a_da_reprovada()
    {
        var linhas = await db.Set<GravacaoTag>().AsNoTracking().ToListAsync();

        Assert.Equal(28, linhas.Count(g => g.Tipo == TipoGravacao.Gravada && g.Resultado == ResultadoGravacao.Ok && g.IdDormente != null));
        Assert.Equal(26, linhas.Count(g => g.Vigente));
        var desligadas = linhas.Where(g => g.Tipo == TipoGravacao.Desligada).ToList();
        Assert.Equal(2, desligadas.Count);
        Assert.All(desligadas, d => Assert.Equal("peça reprovada", d.Observacao));
        Assert.All(desligadas, d => Assert.Contains(linhas, g => g.Tipo == TipoGravacao.Gravada && g.TagGravada == d.TagGravada && !g.Vigente));
    }

    /// <summary>O código da peça fica na coluna DORMENTE.codigo: a busca pelo código vai direto nela.</summary>
    [Fact]
    public async Task O_codigo_da_peca_fica_gravado_e_a_busca_vai_direto_nele()
    {
        var peca = await db.Set<Dormente>().AsNoTracking().OrderBy(d => d.IdDormente).FirstAsync();
        var lote = await db.Set<Lote>().AsNoTracking().SingleAsync(l => l.IdLote == peca.IdLote);

        Assert.Equal(Dormente.MontarCodigo(lote.IdEmpresa, lote.IdLote, peca.IdDormente), peca.Codigo);
        Assert.Equal(28, await db.Set<Dormente>().CountAsync(d => d.Codigo != null));
        Assert.Equal(peca.IdDormente, (await BancoDeTeste.Pecas(db).BuscarPorCodigo($" {peca.Codigo!.ToLowerInvariant()} "))!.Id);
        Assert.Null(await BancoDeTeste.Pecas(db).BuscarPorCodigo("DM-E99-L99-9999"));
    }

    /// <summary>A etiqueta achada em campo: a velha perde a marca e ganha uma Desligada; a nova entra Ligada e vigente. A linha velha só muda a marca.</summary>
    [Fact]
    public async Task Ligar_a_etiqueta_achada_em_campo_desliga_a_velha()
    {
        var peca3 = await Peca(3);
        var velha = await db.Set<GravacaoTag>().AsNoTracking().SingleAsync(g => g.TagGravada == Tag(3) && g.Vigente);
        var vinculos = new VinculoTagService(new ArmazemRepository(db), new PecaRepository(db), BancoDeTeste.Pecas(db));

        await vinculos.Vincular(new VincularTagDto { IdDormente = peca3, Leitura = new() { Tag = "BR5555555555", IdModo = 2 } }, usuario);

        var linhas = await db.Set<GravacaoTag>().AsNoTracking().Where(g => g.IdDormente == peca3).OrderBy(g => g.IdGravacao).ToListAsync();
        Assert.Equal([TipoGravacao.Gravada, TipoGravacao.Desligada, TipoGravacao.Ligada], linhas.Select(g => g.Tipo));
        var depois = linhas[0];
        Assert.False(depois.Vigente);
        depois.Vigente = true;
        Assert.Equivalent(velha, depois); // só a marca mudou
        Assert.Equal((Tag(3), "trocada pela BR5555555555", false), (linhas[1].TagGravada, linhas[1].Observacao, linhas[1].Vigente));
        Assert.Equal(("BR5555555555", true, usuario, (int?)2), (linhas[2].TagGravada, linhas[2].Vigente, linhas[2].IdUsuario, linhas[2].IdModo));
        Assert.Equal(peca3, (await BancoDeTeste.Pecas(db).BuscarPorTag(Tag(3)))!.Id); // a velha ainda acha a peça
    }

    [Fact]
    public async Task O_historico_traz_as_linhas_da_etiqueta()
    {
        var peca3 = await Peca(3);
        var vinculos = new VinculoTagService(new ArmazemRepository(db), new PecaRepository(db), BancoDeTeste.Pecas(db));
        await vinculos.Vincular(new VincularTagDto { IdDormente = peca3, Leitura = new() { Tag = "BR5555555555" } }, usuario);
        var historico = new HistoricoService(new HistoricoRepository(db), new PecaRepository(db));

        var eventos = (await historico.DaPeca(peca3))!;

        var etiqueta = eventos.Where(e => e.Origem.StartsWith("GRAVACAO_TAG")).ToList();
        Assert.Equal(["Gravação da tag", "Tag desligada", "Vínculo de tag"], etiqueta.Select(e => e.Tipo));
        Assert.Contains("trocada pela BR5555555555", etiqueta[1].Descricao);
        Assert.Contains("BR5555555555", etiqueta[2].Descricao);
        Assert.All(etiqueta, e => Assert.Equal(usuario, e.IdUsuario));
    }
}
