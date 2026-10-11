using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ViaPermanente.Dados.Configuracoes;

/// <summary>Atalho para declarar chave estrangeira sem propriedade de navegação.</summary>
internal static class ModelBuilderExtensoes
{
    public static void Fk<TTabela, TAponta>(this ModelBuilder mb, Expression<Func<TTabela, object?>> coluna)
        where TTabela : class
        where TAponta : class
        => mb.Entity<TTabela>().HasOne<TAponta>().WithMany().HasForeignKey(coluna);
}