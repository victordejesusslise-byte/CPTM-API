using ViaPermanente.Dominio.Catalogos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ViaPermanente.Dados.Configuracoes;
using ViaPermanente.Dados.Helpers;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dados;

/// <summary>
/// O banco ViaPermanente no SQL Server: as 51 tabelas do cérebro do projeto.
/// Cada grupo declara chaves, ligações, índices e carga inicial; aqui ficam as convenções que valem para todos.
/// </summary>
public class ViaPermanenteContext(DbContextOptions<ViaPermanenteContext> options) : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().HaveMaxLength(200);
        configurationBuilder.Properties<decimal>().HavePrecision(12, 4);
    }

    /// <summary>As colunas que ligam um registro à peça: mudou o registro, a peça mudou (para a cópia das linhas no celular).</summary>
    private static readonly string[] LigamAPeca = ["IdDormente", "IdDormenteSaiu", "IdDormenteEntrou"];

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        MarcarPecasAlteradas();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        MarcarPecasAlteradas();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Antes de gravar: toda peça que mudou, ou que tem um registro novo ou alterado (inspeção, tag, movimentação, troca, leitura…), ganha
    /// DORMENTE.data_alteracao = agora, na mesma gravação. A peça que não está carregada entra só com a chave e só essa coluna é atualizada.
    /// </summary>
    private void MarcarPecasAlteradas()
    {
        var agora = DateTime.Now;
        var ids = new HashSet<int>();
        foreach (var registro in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList())
        {
            if (registro.Entity is Dormente peca)
            {
                peca.DataAlteracao = agora;
                continue;
            }
            foreach (var nome in LigamAPeca)
                if (registro.Metadata.FindProperty(nome) is { } propriedade && registro.CurrentValues[propriedade] is int id && id > 0)
                    ids.Add(id);
        }
        foreach (var id in ids)
        {
            var carregada = ChangeTracker.Entries<Dormente>().FirstOrDefault(e => e.Entity.IdDormente == id);
            if (carregada is not null)
            {
                carregada.Entity.DataAlteracao = agora;
                continue;
            }
            var so = Attach(new Dormente { IdDormente = id });
            so.Entity.DataAlteracao = agora;
            so.Property(d => d.DataAlteracao).IsModified = true;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        CatalogosConfiguracao.Aplicar(modelBuilder);
        PessoasConfiguracao.Aplicar(modelBuilder);
        ComprasConfiguracao.Aplicar(modelBuilder);
        ChegadaConfiguracao.Aplicar(modelBuilder);
        PatioConfiguracao.Aplicar(modelBuilder);
        ViaConfiguracao.Aplicar(modelBuilder);
        CampoConfiguracao.Aplicar(modelBuilder);
        AmbientalConfiguracao.Aplicar(modelBuilder);

        AplicarConvencoes(modelBuilder);
    }

    private static void AplicarConvencoes(ModelBuilder modelBuilder)
    {
        foreach (var entidade in modelBuilder.Model.GetEntityTypes())
        {
            var tabela = NomeBancoHelper.Tabela(entidade.ClrType.Name);
            entidade.SetTableName(tabela);

            foreach (var propriedade in entidade.GetProperties())
            {
                var coluna = NomeBancoHelper.Coluna(propriedade.Name);
                propriedade.SetColumnName(coluna);
                AplicarTipo(propriedade, coluna);

                if (coluna is "classe" or "classe_exigida")
                    entidade.AddCheckConstraint($"CK_{tabela}_{coluna}", $"[{coluna}] IN (1, 2)");
                // O comprimento é a lista fixa do código (TamanhoDormente.Todos): o banco só aceita os números dela.
                if (coluna == "id_tamanho")
                    entidade.AddCheckConstraint($"CK_{tabela}_id_tamanho", $"[id_tamanho] IN ({string.Join(", ", TamanhoDormente.Todos.Select(t => t.IdTamanho))})");
            }

            // Tabela de evento só recebe inserção: nada apaga em cascata.
            foreach (var chave in entidade.GetForeignKeys())
                chave.DeleteBehavior = DeleteBehavior.Restrict;

            // O mesmo registro enviado duas vezes pelo celular entra uma vez.
            if (typeof(IRegistroDeCampo).IsAssignableFrom(entidade.ClrType))
            {
                var indice = entidade.AddIndex(entidade.FindProperty(nameof(IRegistroDeCampo.IdCliente))!);
                indice.IsUnique = true;
                indice.SetFilter("[id_cliente] IS NOT NULL");
            }
        }
    }

    private static void AplicarTipo(IMutableProperty propriedade, string coluna)
    {
        var tipo = Nullable.GetUnderlyingType(propriedade.ClrType) ?? propriedade.ClrType;

        if (tipo.IsEnum)
        {
            // Exceção: enum que a configuração pediu como número (HasConversion<int>), como o tipo do leitor (1, 2, 3).
            if (propriedade.GetProviderClrType() == typeof(int))
                return;

            // Enum gravado como texto: o banco mostra "AguardandoPeca", não 2.
            var conversor = (ValueConverter)Activator.CreateInstance(
                typeof(EnumToStringConverter<>).MakeGenericType(tipo), new object?[] { null })!;
            propriedade.SetValueConverter(conversor);
            propriedade.SetMaxLength(30);
        }
        else if (tipo == typeof(string))
        {
            propriedade.SetMaxLength(NomeBancoHelper.TamanhoTexto(coluna));
        }
        else if (tipo == typeof(decimal))
        {
            var (precisao, escala) = NomeBancoHelper.PrecisaoDecimal(coluna);
            propriedade.SetPrecision(precisao);
            propriedade.SetScale(escala);
        }
    }
}