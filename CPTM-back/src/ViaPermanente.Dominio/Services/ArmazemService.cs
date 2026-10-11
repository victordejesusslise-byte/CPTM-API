using ViaPermanente.Dominio.Dtos.Pecas;
using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Models.Chegada;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dominio.Services;

/// <summary>
/// Etapa 06, a entrada: só a peça aprovada na verificação entra no armazém, lida com alvo único,
/// numa pilha do tamanho e da classe dela. O lugar de agora fica em DORMENTE.id_pilha, gravado junto com a MOVIMENTACAO.
/// </summary>
public class ArmazemService(IPecaRepository pecas, ICatalogoRepository catalogo, LeituraService leituras, PecaService consulta, IArmazemRepository armazem)
{
    /// <summary>Troca de pilha: a peça no armazém, lida com alvo único, vai para outra pilha do tamanho e da classe dela. Nulo se não existe.</summary>
    public async Task<PecaDto?> TrocarPilha(int idDormente, TrocarPilhaDto dto, int idUsuario)
    {
        var peca = await consulta.BuscarPorId(idDormente);
        if (peca is null)
            return null;
        if (peca.IdPilha is not int pilhaAtual)
            throw new RegraDeNegocioException("A peça não está numa pilha do armazém.");
        var pilha = (await catalogo.ListarPilhas()).FirstOrDefault(p => p.IdPilha == dto.IdPilha && p.Ativo)
            ?? throw new RegraDeNegocioException("Pilha não encontrada.");
        if (pilha.IdPilha == pilhaAtual)
            throw new RegraDeNegocioException("A peça já está nesta pilha.");
        if (pilha.IdTamanho != peca.IdTamanho || pilha.Classe != peca.Classe)
            throw new RegraDeNegocioException($"A pilha {pilha.Codigo} não é de {FormatoHelper.Metros(peca.ComprimentoM)} de {peca.Classe}ª classe.");
        var tag = await pecas.TagAtiva(idDormente) ?? throw new RegraDeNegocioException("A peça não tem tag ativa.");
        var leitura = await leituras.ConferirPeca(idDormente, tag, dto.Leitura, idUsuario);
        var agora = DateTime.Now;
        await armazem.TrocarPilha(idDormente, pilha.IdPilha, new Movimentacao
        {
            IdDormente = idDormente, Tipo = TipoMovimentacao.TrocaDePilha, IdPilhaOrigem = pilhaAtual, IdPilhaDestino = pilha.IdPilha,
            IdUsuario = idUsuario, IdLeitor = dto.Leitura.IdLeitor, Data = agora, RegistradoEm = agora, RecebidoEm = agora
        }, leitura);
        return await consulta.BuscarPorId(idDormente);
    }

    /// <summary>Devolve nulo se a peça não existe. Sem pilha escolhida, usa a sugerida no armazém da chegada.</summary>
    public async Task<PecaDto?> Guardar(int idDormente, GuardarPecaDto dto, int idUsuario)
    {
        var peca = await consulta.BuscarPorId(idDormente);
        if (peca is null)
            return null;
        if ((await pecas.Verificacao(idDormente))?.Resultado != ResultadoVerificacao.Aprovada)
            throw new RegraDeNegocioException("Só a peça aprovada na verificação de qualidade entra no armazém.");
        if (peca.IdPilha is not null)
            throw new RegraDeNegocioException($"A peça já está na pilha {peca.Pilha} de {peca.Armazem}.");
        var tag = await pecas.TagAtiva(idDormente) ?? throw new RegraDeNegocioException("A peça não tem tag ativa.");

        var pilhas = (await catalogo.ListarPilhas()).Where(p => p.Ativo).ToList();
        Pilha pilha;
        if (dto.IdPilha is int idPilha)
        {
            pilha = pilhas.FirstOrDefault(p => p.IdPilha == idPilha) ?? throw new RegraDeNegocioException("Pilha não encontrada.");
            if (pilha.IdTamanho != peca.IdTamanho || pilha.Classe != peca.Classe)
                throw new RegraDeNegocioException($"A pilha {pilha.Codigo} não é de {FormatoHelper.Metros(peca.ComprimentoM)} de {peca.Classe}ª classe.");
        }
        else
            pilha = pilhas.FirstOrDefault(p => p.IdArmazem == peca.IdArmazem && p.IdTamanho == peca.IdTamanho && p.Classe == peca.Classe)
                ?? throw new RegraDeNegocioException($"{peca.Armazem} não tem pilha de {FormatoHelper.Metros(peca.ComprimentoM)} de {peca.Classe}ª classe.");

        var leitura = await leituras.ConferirPeca(idDormente, tag, dto.Leitura, idUsuario);
        var agora = DateTime.Now;
        await pecas.Guardar(idDormente, pilha.IdPilha, agora, new Movimentacao
        {
            IdDormente = idDormente, Tipo = TipoMovimentacao.Entrada, IdPilhaDestino = pilha.IdPilha, IdUsuario = idUsuario,
            IdLeitor = dto.Leitura.IdLeitor, Data = agora, IdCliente = dto.IdCliente, RegistradoEm = dto.RegistradoEm ?? agora, RecebidoEm = agora
        }, leitura);
        return await consulta.BuscarPorId(idDormente);
    }
}