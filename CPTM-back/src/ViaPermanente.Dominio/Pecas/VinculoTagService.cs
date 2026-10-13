using ViaPermanente.Dominio.Armazens;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas;

/// <summary>
/// Tag sem cadastro em campo: o operador da via avisa (a leitura fica com a peça nula e o número lido); o técnico ou o supervisor
/// vincula a tag à peça, lendo-a no local com o módulo e alvo único. A tag velha fica inativa; a nova entra com quem e quando vinculou.
/// </summary>
public class VinculoTagService(IArmazemRepository armazem, IPecaRepository pecas, PecaService consulta)
{
    public async Task<AvisoTagDto> Avisar(NovoAvisoTagDto dto, int idUsuario)
    {
        var tag = (dto.Tag ?? "").Trim();
        if (tag.Length == 0)
            throw new RegraDeNegocioException("Informe a tag lida.");
        if (await pecas.BuscarTag(tag) is { Ativo: true })
            throw new RegraDeNegocioException($"A tag {tag} já está associada a uma peça: abra a peça pela tag.");
        var agora = DateTime.Now;
        var leitura = new LeituraDormente
        {
            IdUsuario = idUsuario, IdLeitor = dto.IdLeitor, IdModo = dto.IdModo, MetodoCaptura = MetodoCaptura.ModuloUhf, Leitura = true,
            DataLeitura = agora, TagLida = tag, Km = dto.Km, Aviso = true, TagsVistas = 1,
            Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? null : dto.Observacao.Trim(), RegistradoEm = agora, RecebidoEm = agora
        };
        await armazem.RegistrarLeituras([leitura]);
        return new AvisoTagDto(leitura.IdLeitura, tag, leitura.Km, leitura.Observacao, agora, idUsuario);
    }

    public async Task<IReadOnlyList<AvisoTagDto>> Pendentes()
        => (await armazem.AvisosPendentes()).Select(l => new AvisoTagDto(l.IdLeitura, l.TagLida!, l.Km, l.Observacao, l.DataLeitura, l.IdUsuario)).ToList();

    public async Task<PecaDto> Vincular(VincularTagDto dto, int idUsuario)
    {
        if (await pecas.Buscar(dto.IdDormente) is null)
            throw new RegraDeNegocioException("Peça não encontrada.");
        var tag = (dto.Leitura.Tag ?? "").Trim();
        if (tag.Length == 0)
            throw new RegraDeNegocioException("Leia a tag no local.");
        if (dto.Leitura.Metodo != MetodoCaptura.ModuloUhf || AlvoUnico.EhAmbigua(dto.Leitura.Metodo, dto.Leitura.TagsVistas))
            throw new RegraDeNegocioException("Vincular exige ler a tag no local com o módulo e alvo único.");
        if (await pecas.BuscarTag(tag) is not null)
            throw new RegraDeNegocioException($"A tag {tag} já é de uma peça.");
        var antiga = await pecas.TagAtiva(dto.IdDormente);
        var agora = DateTime.Now;
        await armazem.Vincular(new Rfid { IdDormente = dto.IdDormente, TagRfid = tag, Ativo = true, VinculadaEm = agora, IdUsuarioVinculou = idUsuario },
            antiga?.IdRfid,
            new LeituraDormente
            {
                IdDormente = dto.IdDormente, IdLeitor = dto.Leitura.IdLeitor, IdModo = dto.Leitura.IdModo, IdUsuario = idUsuario,
                MetodoCaptura = MetodoCaptura.ModuloUhf, Leitura = true, DataLeitura = agora, TagLida = tag, TagsVistas = 1,
                Observacao = antiga is null ? "Tag vinculada em campo" : $"Tag vinculada em campo no lugar da {antiga.TagRfid}", RegistradoEm = agora, RecebidoEm = agora
            });
        return (await consulta.BuscarPorId(dto.IdDormente))!;
    }
}