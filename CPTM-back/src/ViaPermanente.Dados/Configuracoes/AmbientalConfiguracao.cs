using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Ambiental.Models;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dados.Configuracoes;

/// <summary>INTEGRACAO_LOG, RELATORIO_AMBIENTAL e RELATORIO_AMBIENTAL_ITEM.</summary>
internal static class AmbientalConfiguracao
{
    public static void Aplicar(ModelBuilder mb)
    {
        mb.Entity<IntegracaoLog>(e => e.HasKey(x => x.IdLog));

        mb.Entity<RelatorioAmbiental>(e => e.HasKey(x => x.IdRelatorio));
        mb.Fk<RelatorioAmbiental, Usuario>(x => x.IdUsuario);

        mb.Entity<RelatorioAmbientalItem>(e =>
        {
            e.HasKey(x => x.IdItem);
            e.HasIndex(x => new { x.IdRelatorio, x.IdEspecie }).IsUnique();
        });
        mb.Fk<RelatorioAmbientalItem, RelatorioAmbiental>(x => x.IdRelatorio);
        mb.Fk<RelatorioAmbientalItem, Especie>(x => x.IdEspecie);
    }
}