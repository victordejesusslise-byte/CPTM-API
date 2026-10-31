using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Chegadas;

/// <summary>
/// Etapa 04: a verificação de qualidade, peça a peça, depois da UHF, feita pelo especialista com a peça lida.
/// É conferência visual: a qualidade vem certificada pela inspetora da placa. Só insere, nunca altera.
/// Reprovada: motivo e foto obrigatórios, a tag é desativada e a peça volta ao fornecedor.
/// </summary>
public class QualidadeService(IPecaRepository pecas, LeituraService leituras, PecaService consulta)
{
    /// <summary>Devolve nulo se a peça não existe.</summary>
    public async Task<PecaDto?> Verificar(int idDormente, VerificarPecaDto dto, int idUsuario)
    {
        if (await pecas.Buscar(idDormente) is null)
            return null;
        var reprovada = dto.Resultado == ResultadoVerificacao.Reprovada;
        if (reprovada && (string.IsNullOrWhiteSpace(dto.Motivo) || string.IsNullOrWhiteSpace(dto.FotoArquivo)))
            throw new RegraDeNegocioException("A peça reprovada precisa de motivo e foto.");
        // Com ataque biológico ou rachadura, de qualquer grau, a peça é reprovada (Victor, 2026-10-31: evitar erro humano). A tela já
        // muda o resultado sozinha; o servidor confere de novo, porque não confia só na tela.
        if (!reprovada && (dto.AtaqueBiologico != AtaqueBiologicoVerificacao.Nenhum || dto.Rachadura != RachaduraVerificacao.Nenhuma))
            throw new RegraDeNegocioException("Peça com ataque biológico ou rachadura é reprovada: marque Reprovada, com o motivo e a foto.");
        var tag = await pecas.TagAtiva(idDormente)
            ?? throw new RegraDeNegocioException("A peça não tem tag ativa.");
        if (await pecas.Verificacao(idDormente) is { } feita)
            throw new RegraDeNegocioException($"Esta peça já foi verificada ({(feita.Resultado == ResultadoVerificacao.Aprovada ? "aprovada" : "reprovada")}).");

        var leitura = await leituras.ConferirPeca(idDormente, tag, dto.Leitura, idUsuario);
        var agora = DateTime.Now;
        await pecas.Verificar(new VerificacaoQualidade
        {
            IdDormente = idDormente, IdUsuario = idUsuario, Data = agora,
            AtaqueBiologico = dto.AtaqueBiologico, Rachadura = dto.Rachadura,
            PlacaAntirrachante = dto.PlacaAntirrachante, PlacaConfere = dto.PlacaConfere,
            Resultado = dto.Resultado, Motivo = Limpo(dto.Motivo), FotoArquivo = Limpo(dto.FotoArquivo),
            Destino = reprovada ? DestinoVerificacao.Devolucao : DestinoVerificacao.Armazem,
            IdCliente = dto.IdCliente, RegistradoEm = dto.RegistradoEm ?? agora, RecebidoEm = agora
        }, leitura, reprovada ? tag.IdRfid : null);
        return await consulta.BuscarPorId(idDormente);
    }

    private static string? Limpo(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}