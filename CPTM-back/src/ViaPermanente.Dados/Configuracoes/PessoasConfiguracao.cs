using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Pessoas;

namespace ViaPermanente.Dados.Configuracoes;

/// <summary>PESSOA, SETOR e USUARIO. O e-mail da pessoa é o login, então é único.</summary>
internal static class PessoasConfiguracao
{
    public static void Aplicar(ModelBuilder mb)
    {
        mb.Entity<Pessoa>(e =>
        {
            e.HasKey(x => x.IdPessoa);
            e.HasIndex(x => x.Email).IsUnique().HasFilter("[email] IS NOT NULL");
        });

        mb.Entity<Setor>(e => e.HasKey(x => x.IdSetor));

        mb.Entity<Usuario>(e =>
        {
            e.HasKey(x => x.IdUsuario);
            e.HasIndex(x => x.IdPessoa).IsUnique();
        });
        mb.Fk<Usuario, Pessoa>(x => x.IdPessoa);
    }
}