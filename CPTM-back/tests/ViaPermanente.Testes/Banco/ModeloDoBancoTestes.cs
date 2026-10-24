using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ViaPermanente.Dados;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>Confere o modelo do EF contra o cérebro do projeto, sem precisar do SQL Server.</summary>
public class ModeloDoBancoTestes
{
    private static readonly string[] TabelasDoCerebro =
    [
        "ALERTA", "ARMAZEM", "CLASSIFICACAO_MADEIRA", "CONFERENCIA_ARMAZEM", "CONTRATO",
        "DETECCAO_IMAGEM", "DORMENTE", "EMPRESA", "ESPECIE", "ESTACAO", "EVENTO_POSICAO", "FEIXE", "GRAVACAO_TAG",
        "INSPECAO", "INSPECAO_CORRECAO", "INTEGRACAO_LOG", "LEITOR", "LEITURA_DORMENTE", "LEITURA_OCR", "LEITURA_VIA",
        "LOTE", "MODELO_OCR", "MODO_LEITURA", "MOVIMENTACAO", "NOTA_FISCAL", "NOTA_FISCAL_ITEM", "OCR_CAMPO",
        "OFERTA_FORNECEDOR", "PARAMETRO", "PASSAGEM", "PEDIDO", "PEDIDO_HISTORICO", "PEDIDO_ITEM", "PILHA",
        "POSICAO_ATUAL", "RECEBIMENTO", "RECUSA_TROCA", "RELATORIO_AMBIENTAL", "RELATORIO_AMBIENTAL_ITEM", "RFID",
        "SENSOR", "SETOR", "TAMANHO_DORMENTE", "TIPO_LEITOR", "TRECHO", "TROCA", "USUARIO", "VERIFICACAO_QUALIDADE",
        "VISTORIA_ALERTA"
    ];

    private static IModel Modelo()
    {
        var opcoes = new DbContextOptionsBuilder<ViaPermanenteContext>()
            .UseSqlServer("Server=nao-conecta;Database=x;Trusted_Connection=True") // só monta o modelo
            .Options;
        using var db = new ViaPermanenteContext(opcoes);
        return db.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void Tem_as_49_tabelas_do_cerebro()
    {
        var tabelas = Modelo().GetEntityTypes().Select(e => e.GetTableName()).OrderBy(n => n).ToArray();
        Assert.Equal(TabelasDoCerebro.OrderBy(n => n), tabelas);
    }

    [Fact]
    public void Nenhuma_ligacao_apaga_em_cascata()
        => Assert.All(Modelo().GetEntityTypes().SelectMany(e => e.GetForeignKeys()),
            fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));

    [Fact]
    public void Leitura_via_tem_chave_com_data_e_ninguem_aponta_para_ela()
    {
        var modelo = Modelo();
        var leitura = modelo.FindEntityType(typeof(LeituraVia))!;

        Assert.Equal(["id_leitura_via", "data"], leitura.FindPrimaryKey()!.Properties.Select(p => p.GetColumnName()));
        Assert.Empty(leitura.GetReferencingForeignKeys());
    }

    [Fact]
    public void Uma_troca_aberta_por_peca()
    {
        var indice = Modelo().FindEntityType(typeof(Troca))!.GetIndexes()
            .Single(i => i.GetDatabaseName() == "UX_TROCA_uma_aberta_por_peca");

        Assert.True(indice.IsUnique);
        Assert.Contains("AguardandoPeca", indice.GetFilter());
        Assert.DoesNotContain("Feita", indice.GetFilter());
    }

    [Fact]
    public void Status_da_troca_gravado_como_texto()
    {
        var status = Modelo().FindEntityType(typeof(Troca))!.FindProperty(nameof(Troca.Status))!;
        Assert.Equal(typeof(string), status.GetValueConverter()!.ProviderClrType);
    }
}