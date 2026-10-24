using ViaPermanente.Dominio.Catalogos.Dtos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Catalogos;

/// <summary>Os cadastros de apoio que as telas usam: empresas (fornecedor, tratadora, inspetora), contratos, estações e trechos.</summary>
public class CadastroService(ICadastroRepository cadastros, ICatalogoRepository catalogo)
{
    public async Task<IReadOnlyList<EmpresaDto>> Empresas(PapelEmpresa? papel)
    {
        var medidas = await MedidasDto();
        return (await cadastros.Empresas()).Where(e => papel is null || e.Papel == papel).OrderBy(e => e.Nome)
            .Select(e => new EmpresaDto(e.IdEmpresa, e.Nome, e.Cnpj, e.Papel.ToString(), e.Ativo, medidas[e.IdEmpresa].ToList())).ToList();
    }

    /// <summary>O fornecedor já entra com os padrões de medida dele (pelo menos um: cada item do pedido escolhe um).</summary>
    public async Task<EmpresaDto> NovaEmpresa(NovaEmpresaDto dto)
    {
        var nome = Exigir(dto.Nome, "Informe o nome da empresa.");
        if ((await cadastros.Empresas()).Any(e => e.Papel == dto.Papel && string.Equals(e.Nome, nome, StringComparison.OrdinalIgnoreCase)))
            throw new RegraDeNegocioException($"Já existe {nome} como {dto.Papel}.");
        var medidas = dto.Papel == PapelEmpresa.Fornecedor ? dto.Medidas : [];
        if (dto.Papel == PapelEmpresa.Fornecedor && medidas.Count == 0)
            throw new RegraDeNegocioException("Cadastre pelo menos um padrão de medida (largura × altura) do fornecedor.");
        var especies = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie);
        foreach (var m in medidas)
            ConferirMedida(m, especies);
        if (medidas.GroupBy(m => (m.IdEspecie, m.LarguraM, m.AlturaM)).Any(g => g.Count() > 1))
            throw new RegraDeNegocioException("O mesmo padrão de medida apareceu duas vezes.");

        var empresa = new Empresa { Nome = nome, Cnpj = Limpo(dto.Cnpj), Papel = dto.Papel, Ativo = true };
        await cadastros.Adicionar(empresa);
        foreach (var m in medidas)
            await cadastros.Adicionar(new MedidaFornecedor { IdEmpresa = empresa.IdEmpresa, IdEspecie = m.IdEspecie, LarguraM = m.LarguraM, AlturaM = m.AlturaM });
        return (await Empresas(null)).First(e => e.Id == empresa.IdEmpresa);
    }

    /// <summary>Mais um padrão de medida para o fornecedor. Nulo se a empresa não existe.</summary>
    public async Task<MedidaDto?> NovaMedida(int idEmpresa, NovaMedidaDto dto)
    {
        var empresa = (await cadastros.Empresas()).FirstOrDefault(e => e.IdEmpresa == idEmpresa);
        if (empresa is null)
            return null;
        if (empresa.Papel != PapelEmpresa.Fornecedor)
            throw new RegraDeNegocioException("O padrão de medida é do fornecedor.");
        ConferirMedida(dto, (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie));
        if ((await cadastros.Medidas()).Any(m => m.IdEmpresa == idEmpresa && m.Ativo && m.IdEspecie == dto.IdEspecie && m.LarguraM == dto.LarguraM && m.AlturaM == dto.AlturaM))
            throw new RegraDeNegocioException("Este fornecedor já tem esse padrão de medida.");
        var medida = new MedidaFornecedor { IdEmpresa = idEmpresa, IdEspecie = dto.IdEspecie, LarguraM = dto.LarguraM, AlturaM = dto.AlturaM };
        await cadastros.Adicionar(medida);
        return (await MedidasDto())[idEmpresa].First(m => m.Id == medida.IdMedida);
    }

    /// <summary>Desativar não apaga: o padrão some da escolha do pedido, e os pedidos que já usam continuam com ele. Falso se não existe.</summary>
    public async Task<bool> AtivarMedida(int idMedida, bool ativo)
    {
        if ((await cadastros.Medidas()).All(m => m.IdMedida != idMedida))
            return false;
        await cadastros.AtivarMedida(idMedida, ativo);
        return true;
    }

    private async Task<ILookup<int, MedidaDto>> MedidasDto()
    {
        var especies = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie, e => e.NomeCientifico);
        return (await cadastros.Medidas()).OrderBy(m => m.IdEspecie.HasValue).ThenBy(m => m.LarguraM).ThenBy(m => m.AlturaM)
            .Select(m => new MedidaDto(m.IdMedida, m.IdEmpresa, m.IdEspecie, m.IdEspecie is int e ? especies.GetValueOrDefault(e) : null,
                m.LarguraM, m.AlturaM, m.Ativo, MedidasDoPedido.Rotulo(m)))
            .ToLookup(m => m.IdEmpresa);
    }

    private static void ConferirMedida(NovaMedidaDto m, IReadOnlyDictionary<int, Models.Especie> especies)
    {
        if (m.LarguraM is < 0.05m or > 1m || m.AlturaM is < 0.05m or > 1m)
            throw new RegraDeNegocioException("A largura e a altura vão de 0,05 a 1 m.");
        if (m.IdEspecie is int e && !especies.ContainsKey(e))
            throw new RegraDeNegocioException("Espécie não encontrada.");
    }

    /// <summary>Desativar não apaga: a empresa some das listas de escolha, e o que já foi registrado continua. Falso se não existe.</summary>
    public async Task<bool> AtivarEmpresa(int idEmpresa, bool ativo)
    {
        if ((await cadastros.Empresas()).All(e => e.IdEmpresa != idEmpresa))
            return false;
        await cadastros.AtivarEmpresa(idEmpresa, ativo);
        return true;
    }

    public async Task<IReadOnlyList<ContratoDto>> Contratos(int? idEmpresa)
    {
        var nomes = (await cadastros.Empresas()).ToDictionary(e => e.IdEmpresa, e => e.Nome);
        return (await cadastros.Contratos()).Where(c => idEmpresa is null || c.IdEmpresa == idEmpresa).OrderBy(c => c.NumeroContrato)
            .Select(c => new ContratoDto(c.IdContrato, c.IdEmpresa, nomes.GetValueOrDefault(c.IdEmpresa, ""), c.NumeroAta, c.NumeroContrato, c.VigenciaInicio, c.VigenciaFim)).ToList();
    }

    public async Task<ContratoDto> NovoContrato(NovoContratoDto dto)
    {
        var empresa = (await cadastros.Empresas()).FirstOrDefault(e => e.IdEmpresa == dto.IdEmpresa)
            ?? throw new RegraDeNegocioException("Empresa não encontrada.");
        if (empresa.Papel != PapelEmpresa.Fornecedor)
            throw new RegraDeNegocioException("O contrato é com um fornecedor.");
        var numero = Exigir(dto.NumeroContrato, "Informe o número do contrato (ex.: AR00224-03).");
        if ((await cadastros.Contratos()).Any(c => c.NumeroContrato == numero))
            throw new RegraDeNegocioException($"O contrato {numero} já existe.");
        if (dto.VigenciaFim < dto.VigenciaInicio)
            throw new RegraDeNegocioException("O fim da vigência vem depois do início.");
        var contrato = new Contrato { IdEmpresa = empresa.IdEmpresa, NumeroAta = Exigir(dto.NumeroAta, "Informe a ata (ex.: AR00224)."), NumeroContrato = numero, VigenciaInicio = dto.VigenciaInicio, VigenciaFim = dto.VigenciaFim };
        await cadastros.Adicionar(contrato);
        return (await Contratos(empresa.IdEmpresa)).First(c => c.Id == contrato.IdContrato);
    }

    public async Task<IReadOnlyList<EstacaoDto>> Estacoes()
        => (await cadastros.Estacoes()).OrderBy(e => e.Linha).ThenBy(e => e.Ordem)
            .Select(e => new EstacaoDto(e.IdEstacao, e.Nome, e.Linha, e.Km, e.Ordem)).ToList();

    public async Task<EstacaoDto> NovaEstacao(NovaEstacaoDto dto)
    {
        var nome = Exigir(dto.Nome, "Informe o nome da estação.");
        var linha = Exigir(dto.Linha, "Informe a linha.");
        if ((await cadastros.Estacoes()).Any(e => e.Linha == linha && e.Ordem == dto.Ordem))
            throw new RegraDeNegocioException($"Já existe uma estação na posição {dto.Ordem} da linha {linha}.");
        var estacao = new Estacao { Nome = nome, Linha = linha, Km = dto.Km, Ordem = dto.Ordem };
        await cadastros.Adicionar(estacao);
        return new EstacaoDto(estacao.IdEstacao, nome, linha, dto.Km, dto.Ordem);
    }

    public async Task<IReadOnlyList<TrechoDto>> Trechos()
    {
        var estacoes = (await cadastros.Estacoes()).ToDictionary(e => e.IdEstacao, e => e.Nome);
        return (await cadastros.Trechos())
            .Select(t => new TrechoDto(t.IdTrecho, t.IdEstacaoInicio, estacoes.GetValueOrDefault(t.IdEstacaoInicio, ""), t.IdEstacaoFim, estacoes.GetValueOrDefault(t.IdEstacaoFim, ""), t.ClasseExigida))
            .OrderBy(t => t.EstacaoInicio).ToList();
    }

    public async Task<TrechoDto> NovoTrecho(NovoTrechoDto dto)
    {
        var estacoes = (await cadastros.Estacoes()).ToDictionary(e => e.IdEstacao);
        if (!estacoes.TryGetValue(dto.IdEstacaoInicio, out var inicio) || !estacoes.TryGetValue(dto.IdEstacaoFim, out var fim))
            throw new RegraDeNegocioException("Estação não encontrada.");
        if (inicio.Linha != fim.Linha || Math.Abs(inicio.Ordem - fim.Ordem) != 1)
            throw new RegraDeNegocioException("O trecho é entre duas estações vizinhas da mesma linha.");
        if (dto.ClasseExigida is not (1 or 2))
            throw new RegraDeNegocioException("A classe exigida é 1 ou 2.");
        if ((await cadastros.Trechos()).Any(t => t.IdEstacaoInicio == dto.IdEstacaoInicio && t.IdEstacaoFim == dto.IdEstacaoFim))
            throw new RegraDeNegocioException("Este trecho já existe.");
        var trecho = new Trecho { IdEstacaoInicio = dto.IdEstacaoInicio, IdEstacaoFim = dto.IdEstacaoFim, ClasseExigida = dto.ClasseExigida };
        await cadastros.Adicionar(trecho);
        return (await Trechos()).First(t => t.Id == trecho.IdTrecho);
    }

    private static string Exigir(string? texto, string mensagem) => string.IsNullOrWhiteSpace(texto) ? throw new RegraDeNegocioException(mensagem) : texto.Trim();
    private static string? Limpo(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}