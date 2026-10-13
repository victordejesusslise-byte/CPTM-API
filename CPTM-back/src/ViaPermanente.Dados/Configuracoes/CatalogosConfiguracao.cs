using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dados.Configuracoes;

/// <summary>CODIGO_MATERIAL, ESPECIE, PARAMETRO e TAMANHO_DORMENTE, com a carga inicial.</summary>
internal static class CatalogosConfiguracao
{
    public static void Aplicar(ModelBuilder mb)
    {
        mb.Entity<CodigoMaterial>(e =>
        {
            e.HasKey(x => x.IdCodigo);
            e.HasIndex(x => new { x.IdTamanho, x.Classe }).IsUnique();
        });
        mb.Fk<CodigoMaterial, TamanhoDormente>(x => x.IdTamanho);

        // LISTA DE EXEMPLO, a mesma do protótipo, até chegar a oficial (Lista oficial das 22 espécies).
        mb.Entity<Especie>(e =>
        {
            e.HasKey(x => x.IdEspecie);
            e.HasIndex(x => x.NomeCientifico).IsUnique();
            e.HasData(
                Especie(1, "Ocotea neesiana", "Lauraceae", "Canela", 1),
                Especie(2, "Handroanthus serratifolius", "Bignoniaceae", "Ipê-amarelo", 1),
                Especie(3, "Hymenaea courbaril", "Fabaceae", "Jatobá", 1),
                Especie(4, "Dipteryx odorata", "Fabaceae", "Cumaru", 1),
                Especie(5, "Manilkara huberi", "Sapotaceae", "Maçaranduba", 1),
                Especie(6, "Myracrodruon urundeuva", "Anacardiaceae", "Aroeira", 1),
                Especie(7, "Astronium lecointei", "Anacardiaceae", "Muiracatiara", 1),
                Especie(8, "Handroanthus impetiginosus", "Bignoniaceae", "Ipê-roxo", 1),
                Especie(9, "Bowdichia nitida", "Fabaceae", "Sucupira", 1),
                Especie(10, "Mezilaurus itauba", "Lauraceae", "Itaúba", 1),
                Especie(11, "Dinizia excelsa", "Fabaceae", "Angelim-vermelho", 1),
                Especie(12, "Goupia glabra", "Goupiaceae", "Cupiúba", 1),
                Especie(13, "Corymbia citriodora", "Myrtaceae", "Eucalipto-citriodora", 2),
                Especie(14, "Eucalyptus grandis", "Myrtaceae", "Eucalipto-grandis", 2),
                Especie(15, "Eucalyptus saligna", "Myrtaceae", "Eucalipto-saligna", 2),
                Especie(16, "Corymbia maculata", "Myrtaceae", "Eucalipto-maculata", 2),
                Especie(17, "Vatairea sericea", "Fabaceae", "Angelim-amargoso", 2),
                Especie(18, "Hymenolobium petraeum", "Fabaceae", "Angelim-pedra", 2),
                Especie(19, "Qualea paraensis", "Vochysiaceae", "Mandioqueira", 2),
                Especie(20, "Erisma uncinatum", "Vochysiaceae", "Cedrinho", 2),
                Especie(21, "Apuleia leiocarpa", "Fabaceae", "Garapa", 2),
                Especie(22, "Couratari oblongifolia", "Lecythidaceae", "Tauari", 2));
        });

        mb.Entity<Parametro>(e =>
        {
            e.HasKey(x => x.Chave);
            e.HasData(
                new Parametro { Chave = "limite_segunda_classe_pct", Valor = "20" },
                new Parametro { Chave = "limite_segunda_classe_unidade", Valor = "m3" },
                new Parametro { Chave = "alerta_imagem_confianca_minima", Valor = "0.80" },
                new Parametro { Chave = "troca_quem_aprova", Valor = "supervisor" },
                new Parametro { Chave = "ocr_confianca_minima", Valor = "97" },
                // Diferença aceita entre o m³ declarado na nota e o do catálogo (ConferenciaNota).
                new Parametro { Chave = "nota_tolerancia_volume_pct", Valor = "2" },
                // Provisórios até o Limite de desvio de 40 mm ter dono: acima do limite abre alerta; acima da atenção, faixa de atenção.
                new Parametro { Chave = "desvio_limite_mm", Valor = "40" },
                new Parametro { Chave = "desvio_atencao_mm", Valor = "20" });
        });
        mb.Fk<Parametro, Usuario>(x => x.AlteradoPor);

        // Medidas provisórias até a CPTM confirmar (Medidas reais de cada tamanho).
        mb.Entity<TamanhoDormente>(e =>
        {
            e.HasKey(x => x.IdTamanho);
            e.HasIndex(x => x.ComprimentoM).IsUnique();
            e.HasData(
                new TamanhoDormente { IdTamanho = 1, ComprimentoM = 2.80m, LarguraM = 0.24m, AlturaM = 0.17m, MedidaProvisoria = true, Ativo = true },
                new TamanhoDormente { IdTamanho = 2, ComprimentoM = 3.00m, LarguraM = 0.24m, AlturaM = 0.17m, MedidaProvisoria = true, Ativo = true },
                new TamanhoDormente { IdTamanho = 3, ComprimentoM = 4.00m, LarguraM = 0.24m, AlturaM = 0.17m, MedidaProvisoria = true, Ativo = true },
                new TamanhoDormente { IdTamanho = 4, ComprimentoM = 4.80m, LarguraM = 0.24m, AlturaM = 0.17m, MedidaProvisoria = true, Ativo = true });
        });
    }

    private static Especie Especie(int id, string cientifico, string familia, string popular, byte classe)
        => new() { IdEspecie = id, NomeCientifico = cientifico, Familia = familia, NomePopular = popular, Classe = classe, Ativo = true };
}