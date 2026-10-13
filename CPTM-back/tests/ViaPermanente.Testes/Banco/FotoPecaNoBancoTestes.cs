using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Laudos;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>A foto da entrada no SQL Server e numa pasta de verdade: guarda com o ID da peça e aparece nas fotos da peça.</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class FotoPecaNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    private readonly string pasta = Path.Combine(Path.GetTempPath(), "ybyra-fotos-" + Guid.NewGuid().ToString("N"));

    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public async Task DisposeAsync()
    {
        await BancoDeTeste.Apagar(db);
        if (Directory.Exists(pasta)) Directory.Delete(pasta, true);
    }

    [Fact]
    public async Task Guarda_a_foto_da_entrada_com_o_id_da_peca_e_mostra_nas_fotos_da_peca()
    {
        var usuario = await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Especialista de teste", Email = "especialista@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Especialista
        });
        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(usuario.Id);
        var verificacao = await db.Set<VerificacaoQualidade>().OrderBy(v => v.IdVerificacao).FirstAsync();
        var servico = new FotoPecaService(new FotoRepository(pasta), new FotoPecaRepository(db));

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.SalvarEntrada(verificacao.IdDormente, [1, 2, 3], ".exe"));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.SalvarEntrada(999999, [1, 2, 3], ".jpg"));
        var caminho = await servico.SalvarEntrada(verificacao.IdDormente, [1, 2, 3], ".JPG");
        Assert.StartsWith($"pecas/{verificacao.IdDormente}/entrada/", caminho);
        Assert.Equal([1, 2, 3], servico.Arquivo(caminho));
        Assert.Null(servico.Arquivo("../fora-da-pasta.jpg"));

        await db.Set<VerificacaoQualidade>().Where(v => v.IdVerificacao == verificacao.IdVerificacao).ExecuteUpdateAsync(s => s.SetProperty(v => v.FotoArquivo, caminho));
        var fotos = await servico.DaPeca(verificacao.IdDormente);
        var entrada = Assert.Single(fotos!.Entrada);
        Assert.Equal(caminho, entrada.Arquivo);
        Assert.True(entrada.NoServidor);
        Assert.Null(entrada.AnaliseIa);
        Assert.Null(await servico.DaPeca(999999));
    }
}
