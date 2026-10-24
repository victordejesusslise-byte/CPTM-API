using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ViaPermanente.Dados.Configuracoes;
using ViaPermanente.Dados.Helpers;
using ViaPermanente.Dominio.Comum;

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