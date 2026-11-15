using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dados.Configuracoes;

/// <summary>
/// ARMAZEM, CLASSIFICACAO_MADEIRA, CONFERENCIA_ARMAZEM, DORMENTE, GRAVACAO_TAG, LEITOR, LEITURA_DORMENTE,
/// LOTE, MODO_LEITURA, MOVIMENTACAO, PILHA, SENSOR e TIPO_LEITOR. A RFID saiu (2026-11-14): a etiqueta fica na GRAVACAO_TAG.
/// </summary>
internal static class PatioConfiguracao
{
    public static void Aplicar(ModelBuilder mb)
    {
        // Cinco armazéns de exemplo na Linha 9 (Cinco armazéns, com setor e pilha); nomes e km do protótipo.
        mb.Entity<Armazem>(e =>
        {
            e.HasKey(x => x.IdArmazem);
            e.HasIndex(x => x.Nome).IsUnique();
            e.HasData(
                Armazem(1, "Presidente Altino", 199.800m),
                Armazem(2, "Pinheiros", 209.600m),
                Armazem(3, "Santo Amaro", 219.300m),
                Armazem(4, "Jurubatuba", 224.900m),
                Armazem(5, "Grajaú", 231.500m));
        });

        mb.Entity<ClassificacaoMadeira>(e => e.HasKey(x => x.IdClassificacao));
        mb.Fk<ClassificacaoMadeira, Dormente>(x => x.IdDormente);
        mb.Fk<ClassificacaoMadeira, Especie>(x => x.IdEspecie);
        mb.Fk<ClassificacaoMadeira, Usuario>(x => x.IdUsuario);
        mb.Fk<ClassificacaoMadeira, ClassificacaoMadeira>(x => x.Anterior);

        mb.Entity<ConferenciaArmazem>(e => e.HasKey(x => x.IdConferencia));
        mb.Fk<ConferenciaArmazem, Armazem>(x => x.IdArmazem);
        mb.Fk<ConferenciaArmazem, Usuario>(x => x.IdUsuario);
        mb.Fk<ConferenciaArmazem, Leitor>(x => x.IdLeitor);

        mb.Entity<Dormente>(e =>
        {
            e.HasKey(x => x.IdDormente);
            // A cópia das linhas no celular pede "o que mudou desde…".
            e.HasIndex(x => x.DataAlteracao);
            // O código impresso (DM-E01-L01-0028) é único. Nulo só dentro da transação do cadastro, antes de o número da peça existir.
            e.HasIndex(x => x.Codigo).IsUnique().HasFilter("[codigo] IS NOT NULL");
        });
        mb.Fk<Dormente, Lote>(x => x.IdLote);
        mb.Fk<Dormente, Feixe>(x => x.IdFeixe);
        mb.Fk<Dormente, Trecho>(x => x.IdTrecho);
        mb.Fk<Dormente, Pilha>(x => x.IdPilha);

        // O histórico da etiqueta (Etiqueta na GRAVACAO_TAG, sem a RFID, 2026-11-14). As regras da etiqueta vigente ficam no banco.
        mb.Entity<GravacaoTag>(e =>
        {
            e.HasKey(x => x.IdGravacao);
            e.HasIndex(x => x.TagGravada);
            // Um BR vigente pertence a uma peça só.
            e.HasIndex(x => x.TagGravada, "UX_GRAVACAO_TAG_tag_vigente").IsUnique().HasFilter("[vigente] = 1");
            // Cada peça tem no máximo uma etiqueta vigente.
            e.HasIndex(x => x.IdDormente, "UX_GRAVACAO_TAG_uma_vigente_por_peca").IsUnique().HasFilter("[vigente] = 1");
            // Só pode ser vigente a Gravada ok ou a Ligada, e sempre com a peça.
            e.ToTable(t => t.HasCheckConstraint("CK_GRAVACAO_TAG_vigente",
                "[vigente] = 0 OR ([id_dormente] IS NOT NULL AND ([tipo] = 'Ligada' OR ([tipo] = 'Gravada' AND [resultado] = 'Ok')))"));
        });
        mb.Fk<GravacaoTag, Dormente>(x => x.IdDormente);
        mb.Fk<GravacaoTag, ModoLeitura>(x => x.IdModo);
        mb.Fk<GravacaoTag, Leitor>(x => x.IdLeitor);
        mb.Fk<GravacaoTag, Usuario>(x => x.IdUsuario);

        mb.Entity<Leitor>(e =>
        {
            e.HasKey(x => x.IdLeitor);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.HasIndex(x => x.NumeroSerie).IsUnique().HasFilter("[numero_serie] IS NOT NULL");
            // O tipo do leitor é o número fixo no código (TipoLeitor): 1 Módulo RFID, 2 Pistola NIR, 3 Carro. O banco só aceita esses três.
            e.Property(x => x.Tipo).HasConversion<int>();
            e.ToTable(t => t.HasCheckConstraint("CK_LEITOR_tipo", "[tipo] IN (1, 2, 3)"));
        });
        mb.Fk<Leitor, Setor>(x => x.IdSetor);

        mb.Entity<LeituraDormente>(e =>
        {
            e.HasKey(x => x.IdLeitura);
            e.HasIndex(x => x.TagLida);
        });
        mb.Fk<LeituraDormente, Dormente>(x => x.IdDormente);
        mb.Fk<LeituraDormente, Leitor>(x => x.IdLeitor);
        mb.Fk<LeituraDormente, Usuario>(x => x.IdUsuario);
        mb.Fk<LeituraDormente, ModoLeitura>(x => x.IdModo);
        mb.Fk<LeituraDormente, ConferenciaArmazem>(x => x.IdConferencia);
        mb.Fk<LeituraDormente, Pilha>(x => x.IdPilha);

        mb.Entity<Lote>(e =>
        {
            e.HasKey(x => x.IdLote);
            e.HasIndex(x => x.Codigo).IsUnique();
        });
        mb.Fk<Lote, Empresa>(x => x.IdEmpresa);
        mb.Fk<Lote, Pedido>(x => x.IdPedido);

        // Valores de começo, provisórios até os ensaios (Alcance real do leitor).
        mb.Entity<ModoLeitura>(e =>
        {
            e.HasKey(x => x.IdModo);
            e.HasIndex(x => x.Nome).IsUnique();
            e.HasData(
                new ModoLeitura { IdModo = 1, Nome = "Armazém", PotenciaDbm = 10, FolgaRssiDb = 12, Antena = Antena.Curta, ExigeAlvoUnico = true },
                new ModoLeitura { IdModo = 2, Nome = "Normal", PotenciaDbm = 18, FolgaRssiDb = 9, Antena = Antena.Unica, ExigeAlvoUnico = true },
                new ModoLeitura { IdModo = 3, Nome = "Via", PotenciaDbm = 27, FolgaRssiDb = 6, Antena = Antena.Longa, ExigeAlvoUnico = false },
                new ModoLeitura { IdModo = 4, Nome = "Personalizado", PotenciaDbm = 20, FolgaRssiDb = 8, Antena = Antena.Unica, ExigeAlvoUnico = true });
        });
        mb.Fk<ModoLeitura, Usuario>(x => x.AlteradoPor);

        mb.Entity<Movimentacao>(e => e.HasKey(x => x.IdMovimentacao));
        mb.Fk<Movimentacao, Dormente>(x => x.IdDormente);
        mb.Fk<Movimentacao, Pilha>(x => x.IdPilhaOrigem);
        mb.Fk<Movimentacao, Pilha>(x => x.IdPilhaDestino);
        mb.Fk<Movimentacao, Troca>(x => x.IdTroca);
        mb.Fk<Movimentacao, LoteTroca>(x => x.IdLoteTroca);
        mb.Fk<Movimentacao, LeituraDormente>(x => x.IdLeitura);
        mb.Fk<Movimentacao, Usuario>(x => x.IdUsuario);
        mb.Fk<Movimentacao, Leitor>(x => x.IdLeitor);

        mb.Entity<Pilha>(e =>
        {
            e.HasKey(x => x.IdPilha);
            e.HasIndex(x => new { x.IdArmazem, x.Codigo }).IsUnique();
            e.HasData(PilhasDeExemplo());
        });
        mb.Fk<Pilha, Armazem>(x => x.IdArmazem);

        mb.Entity<Sensor>(e => e.HasKey(x => x.IdSensor));
        mb.Fk<Sensor, Leitor>(x => x.IdLeitor);

    }

    private static Armazem Armazem(int id, string nome, decimal km)
        => new() { IdArmazem = id, Nome = nome, Linha = "9-Esmeralda", Km = km, Ativo = true };

    /// <summary>
    /// Oito pilhas por armazém: o setor vem do comprimento (A 2,80 · B 3,00 · C 4,00 · D 4,80 m) e a pilha,
    /// do setor e da classe (B01 = 3,00 m de 1ª, B02 = de 2ª). O tamanho 1 a 4 é a ordem dos setores.
    /// </summary>
    private static IEnumerable<Pilha> PilhasDeExemplo()
    {
        var setores = new[] { "A", "B", "C", "D" };
        var id = 0;
        for (var armazem = 1; armazem <= 5; armazem++)
            for (var s = 0; s < setores.Length; s++)
                for (byte classe = 1; classe <= 2; classe++)
                    yield return new Pilha
                    {
                        IdPilha = ++id,
                        IdArmazem = armazem,
                        Setor = setores[s],
                        Codigo = $"{setores[s]}0{classe}",
                        IdTamanho = s + 1,
                        Classe = classe,
                        Ativo = true
                    };
    }
}