using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dados.Configuracoes;

/// <summary>FEIXE, LEITURA_OCR, MODELO_OCR, OCR_CAMPO e VERIFICACAO_QUALIDADE.</summary>
internal static class ChegadaConfiguracao
{
    public static void Aplicar(ModelBuilder mb)
    {
        mb.Entity<Feixe>(e => e.HasKey(x => x.IdFeixe));
        mb.Fk<Feixe, Recebimento>(x => x.IdRecebimento);
        mb.Fk<Feixe, Especie>(x => x.IdEspecie);
        mb.Fk<Feixe, TamanhoDormente>(x => x.IdTamanho);
        mb.Fk<Feixe, Empresa>(x => x.IdEmpresaTratamento);
        mb.Fk<Feixe, Empresa>(x => x.IdEmpresaInspecao);

        mb.Entity<LeituraOcr>(e => e.HasKey(x => x.IdLeituraOcr));
        mb.Fk<LeituraOcr, ModeloOcr>(x => x.IdModelo);
        mb.Fk<LeituraOcr, Recebimento>(x => x.IdRecebimento);
        mb.Fk<LeituraOcr, Feixe>(x => x.IdFeixe);
        mb.Fk<LeituraOcr, Usuario>(x => x.IdUsuario);

        mb.Entity<ModeloOcr>(e =>
        {
            e.HasKey(x => x.IdModelo);
            e.HasIndex(x => new { x.Nome, x.Versao }).IsUnique();
            // Só um modelo ativo por vez.
            e.HasIndex(x => x.Ativo).IsUnique().HasFilter("[ativo] = 1").HasDatabaseName("UX_MODELO_OCR_um_ativo");
            // Versão 1: o português padrão do Tesseract (tessdata/por.traineddata), sem treino.
            e.HasData(new ModeloOcr { IdModelo = 1, Nome = "placa-feixe", Versao = 1, ModeloBase = "por", Amostras = 0, Ativo = true });
        });
        mb.Fk<ModeloOcr, Usuario>(x => x.IdUsuarioAtivou);

        mb.Entity<OcrCampo>(e => e.HasKey(x => x.IdCampo));
        mb.Fk<OcrCampo, LeituraOcr>(x => x.IdLeituraOcr);
        mb.Fk<OcrCampo, ModeloOcr>(x => x.IdModeloTreino);

        mb.Entity<VerificacaoQualidade>(e =>
        {
            e.HasKey(x => x.IdVerificacao);
            // Reprovada exige motivo e foto.
            e.ToTable(t => t.HasCheckConstraint("CK_VERIFICACAO_QUALIDADE_reprovada",
                "[resultado] <> 'Reprovada' OR ([motivo] IS NOT NULL AND [foto_arquivo] IS NOT NULL)"));
        });
        mb.Fk<VerificacaoQualidade, Dormente>(x => x.IdDormente);
        mb.Fk<VerificacaoQualidade, Usuario>(x => x.IdUsuario);
    }
}