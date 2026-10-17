using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas;

/// <summary>
/// Classificação da madeira (etapa 07, especialista): confirma por comparação a espécie que veio na placa, ou corrige.
/// Nunca apaga a de antes: a nova aponta para a anterior. A classe da peça vem da espécie da classificação mais nova.
/// </summary>
public class ClassificacaoService(IClassificacaoRepository repo, IPecaRepository pecas)
{
    public async Task<ClassificacaoDto> Classificar(NovaClassificacaoDto dto, int idUsuario)
    {
        if (dto.IdCliente is { } idCliente && await repo.PorCliente(idCliente) is { } repetida)
            return await Dto(repetida);
        if (await pecas.Buscar(dto.IdDormente) is null)
            throw new RegraDeNegocioException("Peça não encontrada.");
        if (string.IsNullOrWhiteSpace(dto.Leitura.Tag))
            throw new RegraDeNegocioException("Leia a tag da peça antes de classificar.");
        var tag = await pecas.BuscarTag(dto.Leitura.Tag.Trim());
        if (tag is null || tag.IdDormente != dto.IdDormente)
            throw new RegraDeNegocioException("A tag lida não é desta peça.");
        if (dto.Metodo == MetodoClassificacao.PlacaFornecedor)
            throw new RegraDeNegocioException("A placa do fornecedor já é a primeira classificação: escolha como você conferiu.");
        if (dto.Metodo == MetodoClassificacao.Nir && dto.UmidadePct is null)
            throw new RegraDeNegocioException("Com a pistola NIR, informe a umidade que ela mediu.");
        if (dto.Metodo != MetodoClassificacao.Nir && (dto.UmidadePct is not null || dto.ConfiancaPct is not null))
            throw new RegraDeNegocioException("A umidade e a confiança são da leitura da pistola NIR.");
        var especie = await repo.Especie(dto.IdEspecie) ?? throw new RegraDeNegocioException("Espécie não encontrada.");
        if (!especie.Ativo)
            throw new RegraDeNegocioException("Espécie fora da lista ativa.");

        var anterior = await repo.Ultima(dto.IdDormente);
        var agora = DateTime.Now;
        var nova = await repo.Adicionar(new ClassificacaoMadeira
        {
            IdDormente = dto.IdDormente, IdEspecie = especie.IdEspecie, IdUsuario = idUsuario, Anterior = anterior?.IdClassificacao,
            Data = agora, Metodo = dto.Metodo, MetodoCaptura = dto.Leitura.Metodo, Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? null : dto.Observacao.Trim(),
            UmidadePct = dto.UmidadePct, ConfiancaPct = dto.ConfiancaPct,
            IdCliente = dto.IdCliente, RegistradoEm = agora, RecebidoEm = agora
        });
        return Dto(nova, especie);
    }

    public async Task<IReadOnlyList<ClassificacaoDto>> DaPeca(int idDormente)
        => (await repo.Listar(idDormente, null)).Select(x => Dto(x.Classificacao, x.Especie)).ToList();

    /// <summary>As feitas pelos especialistas nos últimos dias (sem a da placa).</summary>
    public async Task<IReadOnlyList<ClassificacaoDto>> Recentes(int dias)
        => (await repo.Listar(null, DateTime.Now.AddDays(-Math.Clamp(dias, 1, 365)))).Select(x => Dto(x.Classificacao, x.Especie)).ToList();

    private async Task<ClassificacaoDto> Dto(ClassificacaoMadeira c) => Dto(c, (await repo.Especie(c.IdEspecie))!);

    private static ClassificacaoDto Dto(ClassificacaoMadeira c, Especie e)
        => new(c.IdClassificacao, c.IdDormente, e.IdEspecie, e.NomePopular ?? e.NomeCientifico, c.Metodo.ToString(), c.Data, c.IdUsuario, c.Anterior, c.Observacao, c.UmidadePct, c.ConfiancaPct);
}
