using ViaPermanente.Dominio.Ambiental;
using ViaPermanente.Dominio.Ambiental.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Dtos;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Integracoes.Dtos;
using ViaPermanente.Dominio.Pecas;

namespace ViaPermanente.Dominio.Integracoes;

/// <summary>
/// As planilhas que saem do sistema (Integração só por planilha), com modelo PROVISÓRIO de colunas até chegarem os modelos reais:
/// o relatório ambiental para o site do IBAMA e o estoque peça a peça com o código de material para o ERP. Cada uma fica em INTEGRACAO_LOG.
/// </summary>
public class ExportacaoService(IPlanilhaExportacaoRepository planilha, IIntegracaoLogRepository log, RelatorioAmbientalService relatorios,
    PecaService pecas, ICatalogoRepository catalogo)
{
    /// <summary>O relatório do mês: uma linha por espécie. Nulo se o relatório não existe.</summary>
    public async Task<ArquivoDto?> Ibama(int idRelatorio)
    {
        var relatorio = await relatorios.BuscarPorId(idRelatorio);
        if (relatorio is null)
            return null;
        var familias = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie, e => e.Familia);
        var arquivo = new ArquivoDto($"ibama-{relatorio.MesReferencia:yyyy-MM}.xlsx", planilha.Montar("Estoque por espécie",
            ["Mês", "Espécie", "Família", "Estoque (m³)", "Entrada (m³)", "Saída (m³)", "Idade média (dias)", "Situação do relatório"],
            relatorio.Itens.Select(i => new object?[] { relatorio.MesReferencia, i.Especie, familias.GetValueOrDefault(i.IdEspecie), i.VolumeM3Estoque,
                i.VolumeM3Entrada, i.VolumeM3Saida, i.IdadeMediaDias, relatorio.Status })));
        await Registrar(SistemaIntegracao.Ibama, TipoIntegracao.Relatorio, $"Relatório de {relatorio.MesReferencia:MM/yyyy} ({relatorio.Status}) exportado: {relatorio.Itens.Count} espécie(s).", arquivo.Nome);
        return arquivo;
    }

    /// <summary>O estoque peça a peça, com o código de material do ERP (tamanho e classe). Peça sem código cadastrado sai com o código vazio.</summary>
    public async Task<ArquivoDto> Erp()
    {
        var codigos = (await catalogo.ListarCodigosMaterial()).ToDictionary(c => (c.IdTamanho, c.Classe), c => c.CodigoErp);
        var estoque = await pecas.Estoque();
        var arquivo = new ArquivoDto($"erp-estoque-{DateTime.Now:yyyy-MM-dd}.xlsx", planilha.Montar("Estoque",
            ["Código ERP", "Peça", "Tag", "Comprimento (m)", "Classe", "Espécie", "Armazém", "Pilha", "Entrada no armazém"],
            estoque.Select(p => new object?[] { codigos.GetValueOrDefault((p.IdTamanho, p.Classe)), p.Codigo, p.Tag, p.ComprimentoM, p.Classe,
                p.Especie, p.Armazem, p.Pilha, p.EntradaArmazemEm })));
        var semCodigo = estoque.Count(p => !codigos.ContainsKey((p.IdTamanho, p.Classe)));
        await Registrar(SistemaIntegracao.Erp, TipoIntegracao.CodigoMaterial,
            $"Estoque exportado: {estoque.Count} peça(s){(semCodigo > 0 ? $", {semCodigo} sem código de material cadastrado" : "")}.", arquivo.Nome);
        return arquivo;
    }

    public async Task<IReadOnlyList<CodigoMaterialDto>> CodigosMaterial()
    {
        var tamanhos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho, t => t.ComprimentoM);
        return (await catalogo.ListarCodigosMaterial())
            .Select(c => new CodigoMaterialDto(c.IdCodigo, c.IdTamanho, tamanhos.GetValueOrDefault(c.IdTamanho), c.Classe, c.CodigoErp)).ToList();
    }

    /// <summary>Um código por tamanho e classe.</summary>
    public async Task<CodigoMaterialDto> NovoCodigoMaterial(NovoCodigoMaterialDto dto)
    {
        if ((await catalogo.ListarTamanhos()).All(t => t.IdTamanho != dto.IdTamanho))
            throw new RegraDeNegocioException("Tamanho de dormente não encontrado.");
        if (dto.Classe is not (1 or 2))
            throw new RegraDeNegocioException("A classe é 1 ou 2.");
        if (string.IsNullOrWhiteSpace(dto.CodigoErp))
            throw new RegraDeNegocioException("Informe o código do ERP.");
        if ((await catalogo.ListarCodigosMaterial()).Any(c => c.IdTamanho == dto.IdTamanho && c.Classe == dto.Classe))
            throw new RegraDeNegocioException("Este tamanho e esta classe já têm código de material.");
        var codigo = new CodigoMaterial { IdTamanho = dto.IdTamanho, Classe = dto.Classe, CodigoErp = dto.CodigoErp.Trim() };
        await catalogo.AdicionarCodigoMaterial(codigo);
        return (await CodigosMaterial()).First(c => c.Id == codigo.IdCodigo);
    }

    private Task Registrar(SistemaIntegracao sistema, TipoIntegracao tipo, string mensagem, string arquivo)
        => log.Registrar([new IntegracaoLog { Sistema = sistema, Direcao = DirecaoIntegracao.Saida, Tipo = tipo, Data = DateTime.Now, Status = StatusIntegracao.Ok, Mensagem = mensagem, Arquivo = arquivo }]);
}