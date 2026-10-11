using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Campo;
using ViaPermanente.Dominio.Models.Chegada;
using ViaPermanente.Dominio.Models.Compras;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Models.Via;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

public class IndicadorRepository(ViaPermanenteContext db) : IIndicadorRepository
{
    public async Task<IReadOnlyList<(OrigemAlerta Origem, StatusAlerta Status, TipoAlerta Tipo, string? VersaoModelo)>> AlertasDecididos(DateTime de, DateTime ate)
        => (await (from a in db.Set<Alerta>().AsNoTracking()
                   where a.Status != StatusAlerta.Aberto && a.DataDecisao >= de && a.DataDecisao < ate
                   select new { a.Origem, a.Status, a.Tipo, Versao = db.Set<DeteccaoImagem>().Where(d => d.IdDeteccao == a.IdDeteccao).Select(d => d.VersaoModelo).FirstOrDefault() })
                .ToListAsync()).Select(x => (x.Origem, x.Status, x.Tipo, (string?)x.Versao)).ToList();

    public async Task<IReadOnlyList<(string Campo, bool Corrigido, int Versao)>> CamposOcrConfirmados(DateTime de, DateTime ate)
        => (await (from c in db.Set<OcrCampo>().AsNoTracking()
                   join l in db.Set<LeituraOcr>() on c.IdLeituraOcr equals l.IdLeituraOcr
                   join m in db.Set<ModeloOcr>() on l.IdModelo equals m.IdModelo
                   where c.ValorConfirmado != null && l.Data >= de && l.Data < ate
                   select new { c.Campo, c.Corrigido, m.Versao }).ToListAsync())
            .Select(x => (x.Campo.ToString(), x.Corrigido == true, x.Versao)).ToList();

    public async Task<IReadOnlyList<(int? IdModo, bool Ambigua)>> LeiturasDoModulo(DateTime de, DateTime ate)
        => (await db.Set<LeituraDormente>().AsNoTracking()
                .Where(l => l.MetodoCaptura == MetodoCaptura.ModuloUhf && l.Leitura && l.IdConferencia == null && l.DataLeitura >= de && l.DataLeitura < ate)
                .Select(l => new { l.IdModo, l.Ambigua }).ToListAsync())
            .Select(x => (x.IdModo, x.Ambigua)).ToList();

    public async Task<IReadOnlyList<Troca>> Trocas() => await db.Set<Troca>().AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<(int IdRecebimento, int Informada, int? Nota, int Feixes)>> Chegadas(DateTime de, DateTime ate)
        => (await (from r in db.Set<Recebimento>().AsNoTracking()
                   where r.DataRecebimento >= de && r.DataRecebimento < ate
                   select new
                   {
                       r.IdRecebimento, r.QuantidadeRecebida,
                       Nota = r.IdNota == null ? (int?)null : db.Set<NotaFiscalItem>().Where(i => i.IdNota == r.IdNota).Sum(i => i.Quantidade),
                       Feixes = db.Set<Feixe>().Where(f => f.IdRecebimento == r.IdRecebimento).Sum(f => f.Quantidade)
                   }).ToListAsync())
            .Select(x => (x.IdRecebimento, x.QuantidadeRecebida, x.Nota, x.Feixes)).ToList();

    public async Task<IReadOnlyList<ConferenciaArmazem>> UltimasConferenciasConcluidas()
        => (await db.Set<ConferenciaArmazem>().AsNoTracking().Where(c => c.ConcluidaEm != null).ToListAsync())
            .GroupBy(c => c.IdArmazem).Select(g => g.OrderByDescending(c => c.ConcluidaEm).First()).ToList();

    // Conta no SQL Server: LEITURA_VIA tem milhões de linhas.
    public async Task<IReadOnlyList<(int IdDormente, int Noites)>> FalhasDeTag(DateTime desde)
        => (await db.Set<LeituraVia>().AsNoTracking()
                .Where(l => !l.Leitura && l.IdDormente != null && l.Data >= desde)
                .GroupBy(l => l.IdDormente!.Value)
                .Select(g => new { IdDormente = g.Key, Noites = g.Select(l => l.IdPassagem).Distinct().Count() })
                .ToListAsync())
            .Select(x => (x.IdDormente, x.Noites)).ToList();

    public async Task<IReadOnlyList<ResultadoGravacao>> Gravacoes(DateTime de, DateTime ate)
        => await db.Set<GravacaoTag>().AsNoTracking().Where(g => g.Data >= de && g.Data < ate).Select(g => g.Resultado).ToListAsync();

    public async Task<IReadOnlyList<Inspecao>> Inspecoes(DateTime de, DateTime ate)
        => await db.Set<Inspecao>().AsNoTracking().Where(i => i.FinalizadoEm >= de && i.FinalizadoEm < ate).ToListAsync();

    public async Task<IReadOnlyList<bool>> RevisoesDaClassificacaoVisual(DateTime de, DateTime ate)
        => await (from r in db.Set<ClassificacaoMadeira>().AsNoTracking()
                  join a in db.Set<ClassificacaoMadeira>() on r.Anterior equals a.IdClassificacao
                  where (r.Metodo == MetodoClassificacao.Laboratorio || r.Metodo == MetodoClassificacao.Catalogo)
                        && a.Metodo == MetodoClassificacao.Visual && r.Data >= de && r.Data < ate
                  select r.IdEspecie == a.IdEspecie).ToListAsync();

    public async Task<IReadOnlyList<(int IdEmpresa, bool Reprovada)>> VerificacoesPorFornecedor(DateTime de, DateTime ate)
        => (await (from v in db.Set<VerificacaoQualidade>().AsNoTracking()
                   join d in db.Set<Dormente>() on v.IdDormente equals d.IdDormente
                   join l in db.Set<Lote>() on d.IdLote equals l.IdLote
                   where v.Data >= de && v.Data < ate
                   select new { l.IdEmpresa, Reprovada = v.Resultado == ResultadoVerificacao.Reprovada }).ToListAsync())
            .Select(x => (x.IdEmpresa, x.Reprovada)).ToList();
}