using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Models.Via;

namespace ViaPermanente.Dados.Configuracoes;

/// <summary>DETECCAO_IMAGEM, ESTACAO, EVENTO_POSICAO, LEITURA_VIA, PASSAGEM, POSICAO_ATUAL e TRECHO.</summary>
internal static class ViaConfiguracao
{
    public static void Aplicar(ModelBuilder mb)
    {
        mb.Entity<DeteccaoImagem>(e =>
        {
            e.HasKey(x => x.IdDeteccao);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_DETECCAO_IMAGEM_grau", "[grau] BETWEEN 1 AND 3");
                t.HasCheckConstraint("CK_DETECCAO_IMAGEM_confianca", "[confianca] BETWEEN 0 AND 1");
            });
        });
        mb.Fk<DeteccaoImagem, Passagem>(x => x.IdPassagem);
        mb.Fk<DeteccaoImagem, Dormente>(x => x.IdDormente);

        mb.Entity<Estacao>(e =>
        {
            e.HasKey(x => x.IdEstacao);
            e.HasIndex(x => new { x.Linha, x.Ordem }).IsUnique();
        });

        mb.Entity<EventoPosicao>(e => e.HasKey(x => x.IdEvento));
        mb.Fk<EventoPosicao, Dormente>(x => x.IdDormente);
        mb.Fk<EventoPosicao, Passagem>(x => x.IdPassagem);

        // Chave com a data, pronta para a partição semanal (que entra numa migração própria).
        mb.Entity<LeituraVia>(e =>
        {
            e.HasKey(x => new { x.IdLeituraVia, x.Data });
            e.Property(x => x.IdLeituraVia).UseIdentityColumn();
            e.HasIndex(x => new { x.IdDormente, x.Data });
        });
        mb.Fk<LeituraVia, Passagem>(x => x.IdPassagem);
        mb.Fk<LeituraVia, Dormente>(x => x.IdDormente);

        mb.Entity<Passagem>(e =>
        {
            e.HasKey(x => x.IdPassagem);
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        // Uma linha por peça: a chave é a própria peça.
        mb.Entity<PosicaoAtual>(e =>
        {
            e.HasKey(x => x.IdDormente);
            e.Property(x => x.IdDormente).ValueGeneratedNever();
            e.HasOne<Dormente>().WithOne().HasForeignKey<PosicaoAtual>(x => x.IdDormente);
        });
        mb.Fk<PosicaoAtual, Passagem>(x => x.IdPassagem);

        mb.Entity<Trecho>(e =>
        {
            e.HasKey(x => x.IdTrecho);
            e.HasIndex(x => new { x.IdEstacaoInicio, x.IdEstacaoFim }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_TRECHO_estacoes_diferentes", "[id_estacao_inicio] <> [id_estacao_fim]"));
        });
        mb.Fk<Trecho, Estacao>(x => x.IdEstacaoInicio);
        mb.Fk<Trecho, Estacao>(x => x.IdEstacaoFim);
    }
}