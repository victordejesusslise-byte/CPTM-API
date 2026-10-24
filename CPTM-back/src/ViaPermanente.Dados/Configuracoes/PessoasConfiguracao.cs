using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dados.Configuracoes;

/// <summary>SETOR e USUARIO. O e-mail do usuário é o login, então é único. (A PESSOA entrou no USUARIO em 2026-10-23.)</summary>
internal static class PessoasConfiguracao
{
    public static void Aplicar(ModelBuilder mb)
    {
        mb.Entity<Setor>(e => e.HasKey(x => x.IdSetor));

        mb.Entity<Usuario>(e =>
        {
            e.HasKey(x => x.IdUsuario);
            e.Property(x => x.Nome).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(200);
            e.HasIndex(x => x.Email).IsUnique();
        });
    }
}
