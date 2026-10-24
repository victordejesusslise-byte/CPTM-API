using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dados.Configuracoes;

/// <summary>ALERTA, INSPECAO, INSPECAO_CORRECAO, RECUSA_TROCA, TROCA e VISTORIA_ALERTA.</summary>
internal static class CampoConfiguracao
{
    public static void Aplicar(ModelBuilder mb)
    {
        mb.Entity<Alerta>(e =>
        {
            e.HasKey(x => x.IdAlerta);
            // Sem chave estrangeira para LEITURA_VIA: a leitura é descartada depois de 90 dias e o alerta fica.
            e.HasIndex(x => x.IdLeituraVia);
            e.ToTable(t => t.HasCheckConstraint("CK_ALERTA_origem",
                "([origem] = 'Sensor' AND [id_leitura_via] IS NOT NULL) OR ([origem] = 'Imagem' AND [id_deteccao] IS NOT NULL)"));
        });
        mb.Fk<Alerta, DeteccaoImagem>(x => x.IdDeteccao);
        mb.Fk<Alerta, Dormente>(x => x.IdDormente);
        mb.Fk<Alerta, Usuario>(x => x.IdUsuarioDecisao);

        mb.Entity<Inspecao>(e => e.HasKey(x => x.IdInspecao));
        mb.Fk<Inspecao, Dormente>(x => x.IdDormente);
        mb.Fk<Inspecao, Usuario>(x => x.IdUsuario);
        mb.Fk<Inspecao, Alerta>(x => x.IdAlerta);
        mb.Fk<Inspecao, Inspecao>(x => x.IdInspecaoOrigem);

        mb.Entity<InspecaoCorrecao>(e => e.HasKey(x => x.IdCorrecao));
        mb.Fk<InspecaoCorrecao, Inspecao>(x => x.IdInspecao);
        mb.Fk<InspecaoCorrecao, Usuario>(x => x.IdUsuario);
        mb.Fk<InspecaoCorrecao, Troca>(x => x.IdTrocaCancelada);

        mb.Entity<RecusaTroca>(e =>
        {
            e.HasKey(x => x.IdRecusa);
            e.ToTable(t => t.HasCheckConstraint("CK_RECUSA_TROCA_manter_com_resposta",
                "[decisao] IS NULL OR [decisao] <> 'Manter' OR [resposta] IS NOT NULL"));
        });
        mb.Fk<RecusaTroca, Troca>(x => x.IdTroca);
        mb.Fk<RecusaTroca, Usuario>(x => x.IdUsuario);
        mb.Fk<RecusaTroca, Usuario>(x => x.IdUsuarioDecisao);

        mb.Entity<Troca>(e =>
        {
            e.HasKey(x => x.IdTroca);
            // Uma troca aberta por peça (no MySQL era a coluna gerada; no SQL Server, índice filtrado).
            e.HasIndex(x => x.IdDormenteSaiu).IsUnique()
                .HasFilter("[status] IN ('Pedida', 'Aprovada', 'AguardandoPeca', 'Separada', 'Recusada')")
                .HasDatabaseName("UX_TROCA_uma_aberta_por_peca");
            // A execução enviada duas vezes pelo celular entra uma vez.
            e.HasIndex(x => x.IdCliente).IsUnique().HasFilter("[id_cliente] IS NOT NULL").HasDatabaseName("UX_TROCA_id_cliente");
        });
        mb.Fk<Troca, Dormente>(x => x.IdDormenteSaiu);
        mb.Fk<Troca, Dormente>(x => x.IdDormenteEntrou);
        mb.Fk<Troca, Inspecao>(x => x.IdInspecao);
        mb.Fk<Troca, Trecho>(x => x.IdTrecho);
        mb.Fk<Troca, Usuario>(x => x.IdUsuarioPediu);
        mb.Fk<Troca, Usuario>(x => x.IdUsuarioAprovou);
        mb.Fk<Troca, Usuario>(x => x.IdUsuarioSeparou);
        mb.Fk<Troca, Usuario>(x => x.IdUsuarioFez);

        mb.Entity<VistoriaAlerta>(e => e.HasKey(x => x.IdVistoria));
        mb.Fk<VistoriaAlerta, Alerta>(x => x.IdAlerta);
        mb.Fk<VistoriaAlerta, Usuario>(x => x.IdUsuario);
    }
}