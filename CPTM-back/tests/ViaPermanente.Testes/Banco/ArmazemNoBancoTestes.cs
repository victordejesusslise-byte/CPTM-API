using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Repositories;
using ViaPermanente.Dominio.Dtos.Armazem;
using ViaPermanente.Dominio.Dtos.Pecas;
using ViaPermanente.Dominio.Dtos.Usuarios;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Testes.Banco;

/// <summary>Troca de pilha, conferência pilha a pilha e tag sem cadastro em campo, no SQL Server de verdade (peças do exemplo, em Santo Amaro A01).</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class ArmazemNoBancoTestes : IAsyncLifetime
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
    private async Task<int> Peca(int n) => (await db.Set<Rfid>().AsNoTracking().SingleAsync(r => r.TagRfid == Tag(n))).IdDormente;
    private async Task<int> PilhaId(int armazem, string codigo) => (await db.Set<Pilha>().AsNoTracking().SingleAsync(p => p.IdArmazem == armazem && p.Codigo == codigo)).IdPilha;
    private ArmazemService Armazem() { db.ChangeTracker.Clear(); return new(new PecaRepository(db), new CatalogoRepository(db), new LeituraService(new PecaRepository(db)), BancoDeTeste.Pecas(db), new ArmazemRepository(db)); }
    private ConferenciaService Conferencias() { db.ChangeTracker.Clear(); return new(new ArmazemRepository(db), new CatalogoRepository(db)); }

    [Fact]
    public async Task Troca_de_pilha_e_conferencia_com_falta_e_sobra()
    {
        var peca1 = await Peca(1);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Armazem().TrocarPilha(peca1, new TrocarPilhaDto { IdPilha = 0 + 0, Leitura = new() { Tag = Tag(1) } }, usuario));
        var b01 = await PilhaId(3, "B01");
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Armazem().TrocarPilha(peca1, new TrocarPilhaDto { IdPilha = b01, Leitura = new() { Tag = Tag(1) } }, usuario));
        var movida = await Armazem().TrocarPilha(peca1, new TrocarPilhaDto { IdPilha = await PilhaId(2, "A01"), Leitura = new() { Tag = Tag(1) } }, usuario);
        Assert.Equal(("Pinheiros", "A01"), (movida!.Armazem, movida.Pilha));

        var c = (await Conferencias().Iniciar(3, null, usuario))!;
        await Conferencias().LerPilha(c.Id, new LerPilhaDto { IdPilha = await PilhaId(3, "A01"), Tags = [Tag(1), Tag(2), Tag(3), Tag(4), Tag(5), "BR7777777777"] }, usuario);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Conferencias().Concluir(c.Id)); // faltam 7 pilhas
        foreach (var codigo in new[] { "A02", "B01", "B02", "C01", "C02", "D01", "D02" })
            await Conferencias().LerPilha(c.Id, new LerPilhaDto { IdPilha = await PilhaId(3, codigo) }, usuario);
        var concluida = (await Conferencias().Concluir(c.Id))!;

        Assert.Empty(concluida.PilhasFaltando);
        Assert.Equal((25, 5), (concluida.Esperadas, concluida.Lidas));
        Assert.Equal(21, concluida.Faltando.Count);
        Assert.Equal(Tag(1), Assert.Single(concluida.AMais).Tag);
        Assert.Equal(["BR7777777777"], concluida.TagsSemCadastro);
        var gravada = await db.Set<ConferenciaArmazem>().AsNoTracking().SingleAsync();
        Assert.Equal((25, 5, 21, 1), (gravada.Esperadas!.Value, gravada.Lidas!.Value, gravada.Faltando!.Value, gravada.AMais!.Value));
    }

    [Fact]
    public async Task Tag_achada_em_campo_e_avisada_e_vinculada()
    {
        var vinculos = new VinculoTagService(new ArmazemRepository(db), new PecaRepository(db), BancoDeTeste.Pecas(db));
        await vinculos.Avisar(new NovoAvisoTagDto { Tag = "BR5555555555", Km = 219.410m, Observacao = "Peça com tag nova sem cadastro" }, usuario);
        Assert.Single(await vinculos.Pendentes());
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => vinculos.Vincular(new VincularTagDto { IdDormente = 1, Leitura = new() { Tag = "BR5555555555", TagsVistas = 2 } }, usuario));

        var peca3 = await Peca(3);
        var vinculada = await vinculos.Vincular(new VincularTagDto { IdDormente = peca3, Leitura = new() { Tag = "BR5555555555" } }, usuario);

        Assert.Equal("BR5555555555", vinculada.Tag);
        Assert.False((await db.Set<Rfid>().AsNoTracking().SingleAsync(r => r.TagRfid == Tag(3))).Ativo);
        Assert.Equal(usuario, (await db.Set<Rfid>().AsNoTracking().SingleAsync(r => r.TagRfid == "BR5555555555")).IdUsuarioVinculou);
        Assert.Empty(await vinculos.Pendentes());
    }
}