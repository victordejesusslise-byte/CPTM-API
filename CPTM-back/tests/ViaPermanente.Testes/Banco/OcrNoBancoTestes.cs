using ViaPermanente.Dados;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Chegadas;
using ViaPermanente.Dados.Laudos;
using ViaPermanente.Dados.Pedidos;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Chegadas.Dtos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Dtos;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>O OCR de verdade (Tesseract, português padrão) na placa de exemplo e na foto da placa real, com a conferência e a confirmação no SQL Server.</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class OcrNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    private readonly string fotos = Path.Combine(Path.GetTempPath(), "via-permanente-fotos-teste");
    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public async Task DisposeAsync() { await BancoDeTeste.Apagar(db); if (Directory.Exists(fotos)) Directory.Delete(fotos, true); }

    private static string Tessdata()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (Directory.Exists(Path.Combine(d.FullName, "tessdata"))) return Path.Combine(d.FullName, "tessdata");
        throw new DirectoryNotFoundException("tessdata");
    }

    /// <summary>Pedido do TG–Três Guri, ata AR00224, contrato AR00224-03: Ocotea neesiana de 1ª, 2,80 m.</summary>
    private async Task<(OcrPlacaService Servico, int IdPedido, int IdUsuario)> Preparar()
    {
        var idUsuario = (await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto { Nome = "O", Email = "o@teste.local", Senha = "senha-boa-1", Cargo = Cargo.OperadorPatio })).Id;
        var fornecedor = new Empresa { Nome = "TG–Três Guri", Papel = PapelEmpresa.Fornecedor, Ativo = true };
        db.Add(fornecedor);
        await db.SaveChangesAsync();
        var contrato = new Contrato { IdEmpresa = fornecedor.IdEmpresa, NumeroAta = "AR00224", NumeroContrato = "AR00224-03" };
        db.Add(contrato);
        await db.SaveChangesAsync();
        var ofertas = new OfertaService(new OfertaRepository(db), new PedidoRepository(db), new CatalogoRepository(db));
        var pedido = await new PedidoService(new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db), ofertas).Criar(new NovoPedidoDto
        {
            Codigo = "PED-OCR", IdEmpresa = fornecedor.IdEmpresa, IdContrato = contrato.IdContrato, Itens = [new() { IdTamanho = 1, Classe = 1, IdEspecie = 1, Quantidade = 28 }]
        }, idUsuario);
        var servico = new OcrPlacaService(new TesseractOcrRepository(Tessdata()), new FotoRepository(fotos), new LeituraOcrRepository(db),
            new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db));
        return (servico, pedido.Id, idUsuario);
    }

    private static Task<byte[]> Foto(string nome) => File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Recursos", nome));

    [Fact]
    public async Task Le_a_placa_confere_com_o_pedido_e_confirma_uma_vez()
    {
        var (servico, idPedido, idUsuario) = await Preparar();

        var leitura = await servico.Ler(await Foto("placa-exemplo.png"), ".png", idPedido, idUsuario);
        var campos = leitura.Campos.ToDictionary(c => c.Campo);

        Assert.True(campos.Count >= 7, $"campos lidos: {string.Join(", ", campos.Keys)} · texto: {leitura.TextoLido}");
        Assert.True(campos["Fornecedor"].ConferePedido);
        Assert.Contains("AR00224-03", campos["Contrato"].ValorLido);
        Assert.True(campos["Contrato"].ConferePedido);
        Assert.True(campos["Comprimento"].ConferePedido);
        Assert.Null(campos["Tratamento"].ConferePedido); // sem pedido
        Assert.True(File.Exists(Path.Combine(fotos, leitura.FotoArquivo)));

        var confirmada = (await servico.Confirmar(leitura.Id, new ConfirmarPlacaDto { Campos = [new() { Campo = "Ano", Valor = "2026" }] }))!;
        Assert.True(confirmada.Confirmada);
        Assert.True(confirmada.Campos.Single(c => c.Campo == "Ano").Corrigido);
        Assert.False(confirmada.Campos.Single(c => c.Campo == "Fornecedor").Corrigido);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.Confirmar(leitura.Id, new ConfirmarPlacaDto()));
    }

    /// <summary>
    /// A foto da placa de verdade (2026-09-26): tabela de duas colunas sem dois-pontos, presa numa chapa perfurada, fornecedor em letra
    /// branca. A leitura da foto inteira devolvia "placa lida" sem nenhum campo; lendo célula por célula, saem os nove.
    /// </summary>
    [Fact]
    public async Task Le_a_foto_da_placa_real()
    {
        var (servico, idPedido, idUsuario) = await Preparar();

        var leitura = await servico.Ler(await Foto("placa-real.png"), ".png", idPedido, idUsuario);
        var campos = leitura.Campos.ToDictionary(c => c.Campo);

        var lidos = string.Join(" · ", leitura.Campos.Select(c => $"{c.Campo}={c.ValorLido}"));
        Assert.True(campos.Count == 9, $"campos lidos: {lidos} · texto: {leitura.TextoLido}");
        foreach (var campo in new[] { "Fornecedor", "Ata", "Contrato", "EspecieClasse", "Comprimento" })
            Assert.True(campos[campo].ConferePedido, $"{campo} não conferiu: {lidos}");
        Assert.Equal("AR00224", campos["Ata"].ValorLido);
        Assert.Equal("AR00224-03", campos["Contrato"].ValorLido);
        Assert.Equal("CCA", campos["Tratamento"].ValorLido);
        Assert.Equal("GRANOSKI", campos["Tratadora"].ValorLido);
        Assert.Equal("UFV", campos["Inspetora"].ValorLido);
        Assert.Equal("2025", campos["Ano"].ValorLido);
    }
}
