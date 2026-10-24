using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dados.Configuracoes;

/// <summary>CODIGO_MATERIAL, ESPECIE, PARAMETRO e TAMANHO_DORMENTE, com a carga inicial.</summary>
internal static class CatalogosConfiguracao
{
    public static void Aplicar(ModelBuilder mb)
    {
        // LISTA DE EXEMPLO, a mesma do protótipo, até chegar a oficial (Lista oficial das 22 espécies).
        mb.Entity<Especie>(e =>
        {
            e.HasKey(x => x.IdEspecie);
            e.HasIndex(x => x.NomeCientifico).IsUnique();
            e.HasData(
                // A lista oficial das 22 espécies da CPTM (formulário de retirada, Victor, 2026-10-13). Sem classe: a classe vem do pedido (2026-10-16);
                // os ids das que já existiam ficaram iguais (Ocotea 1, Hymenaea 3, Dipteryx odorata 4, Bowdichia nitida 9, ...).
                Especie(1, "Ocotea neesiana", "Lauraceae", "Canela"),
                Especie(2, "Tabebuia serratifolia", "Bignoniaceae", "Ipê-amarelo"),
                Especie(3, "Hymenaea courbaril", "Fabaceae", "Jatobá"),
                Especie(4, "Dipteryx odorata", "Fabaceae", "Cumaru"),
                Especie(5, "Apuleia molaris", "Fabaceae", "Amarelão"),
                Especie(6, "Aspidosperma polyneuron", "Apocynaceae", "Peroba-rosa"),
                Especie(7, "Astronium graveolens", "Anacardiaceae", "Guaritá"),
                Especie(8, "Bowdichia brasiliensis ou Bowdichia spp", "Fabaceae", "Sucupira"),
                Especie(9, "Bowdichia nitida", "Fabaceae", "Sucupira"),
                Especie(10, "Mezilaurus itauba", "Lauraceae", "Itaúba"),
                Especie(11, "Angelim Vermelho (Promap - AMV)", "Fabaceae", "Angelim-vermelho"),
                Especie(12, "Goupia glabra", "Goupiaceae", "Cupiúba"),
                Especie(13, "Bowdichia racemosa", "Fabaceae", "Sucupira"),
                Especie(14, "Dialium guianense", "Fabaceae", "Jutaí-pororoca"),
                Especie(15, "Diplotropis purpurea", "Fabaceae", "Sucupira-preta"),
                Especie(16, "Dipteryx spp.", "Fabaceae", "Cumaru"),
                Especie(17, "Enterolobium schomburgkii", "Fabaceae", "Orelha-de-macaco"),
                Especie(18, "Hymenolobium petraeum ou Hymenolobium spp", "Fabaceae", "Angelim-pedra"),
                Especie(19, "Hymenaea intermedia", "Fabaceae", "Jatobá"),
                Especie(20, "Coimal (Dormentes antigos - AMV)", "A confirmar", null),
                Especie(21, "Apuleia leiocarpa", "Fabaceae", "Garapa"),
                Especie(22, "Veneer (Dormentes antigos - AMV)", "A confirmar", null));
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
                new TamanhoDormente { IdTamanho = 1, ComprimentoM = 2.80m, LarguraM = 0.24m, AlturaM = 0.17m, MedidaProvisoria = true },
                new TamanhoDormente { IdTamanho = 2, ComprimentoM = 3.00m, LarguraM = 0.24m, AlturaM = 0.17m, MedidaProvisoria = true },
                new TamanhoDormente { IdTamanho = 3, ComprimentoM = 4.00m, LarguraM = 0.24m, AlturaM = 0.17m, MedidaProvisoria = true },
                new TamanhoDormente { IdTamanho = 4, ComprimentoM = 4.80m, LarguraM = 0.24m, AlturaM = 0.17m, MedidaProvisoria = true });
        });
    }

    private static Especie Especie(int id, string cientifico, string familia, string? popular)
        => new() { IdEspecie = id, NomeCientifico = cientifico, Familia = familia, NomePopular = popular, Ativo = true };
}