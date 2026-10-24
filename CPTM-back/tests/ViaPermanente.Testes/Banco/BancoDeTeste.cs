using ViaPermanente.Dominio.Catalogos.Models;
using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Campo;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Chegadas;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Pedidos;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Laudos;
using ViaPermanente.Dominio.Pecas;

namespace ViaPermanente.Testes.Banco;

/// <summary>
/// O banco ViaPermanente_Testes no SQL Server, criado pelas migrações antes de cada teste e apagado depois.
/// A conexão pode vir da variável VIA_TESTE_CONEXAO. As classes da coleção "Banco" rodam uma de cada vez.
/// </summary>
internal static class BancoDeTeste
{
    public static string Conexao => Environment.GetEnvironmentVariable("VIA_TESTE_CONEXAO")
        ?? @"Server=localhost\SQLEXPRESS;Database=ViaPermanente_Testes;Trusted_Connection=True;TrustServerCertificate=True";

    public static ViaPermanenteContext Contexto() => new(new DbContextOptionsBuilder<ViaPermanenteContext>()
        .UseSqlServer(Conexao)
        .Options);

    public static PecaService Pecas(ViaPermanenteContext db) => new(new PecaRepository(db), new CatalogoRepository(db));

    public static TrocaService Trocas(ViaPermanenteContext db) => new(new TrocaRepository(db), new PecaRepository(db), new InspecaoRepository(db),
        new ParametroRepository(db), new CatalogoRepository(db), new LeituraService(new PecaRepository(db)), Pecas(db));

    public static InspecaoService Inspecoes(ViaPermanenteContext db) => new(new InspecaoRepository(db), new AlertaRepository(db), new PecaRepository(db),
        new LeituraService(new PecaRepository(db)), new TrocaRepository(db), Trocas(db));

    public static AlertaService Alertas(ViaPermanenteContext db) => new(new AlertaRepository(db), new PecaRepository(db), new LeituraService(new PecaRepository(db)), Pecas(db));

    public static LaudoService Laudos(ViaPermanenteContext db, ViaPermanente.Dominio.Laudos.IDocumentoPdfRepository pdf) => new(pdf, new PecaRepository(db), new HistoricoRepository(db),
        new UsuarioRepository(db), new TrocaRepository(db), new ChegadaService(new ChegadaRepository(db), new PedidoRepository(db), new NotaFiscalRepository(db), new CatalogoRepository(db), new LeituraOcrRepository(db)),
        Pecas(db), Inspecoes(db), Alertas(db), Trocas(db));

    public static async Task Recriar(ViaPermanenteContext db)
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public static async Task Apagar(ViaPermanenteContext db)
    {
        await db.Database.EnsureDeletedAsync();
        await db.DisposeAsync();
    }
}