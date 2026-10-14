using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas;

/// <summary>
/// A leitura que abre uma operação na peça (Classificar só peça lida, Nada muda de estado sem alvo único).
/// Toda leitura fica em LEITURA_DORMENTE: a ambígua e a de outra peça são gravadas e a operação é recusada.
/// </summary>
public class LeituraService(IPecaRepository pecas)
{
    /// <summary>
    /// Confere que a leitura é desta peça, com alvo único. Devolve a leitura pronta para gravar junto com a operação;
    /// se não confere, grava a leitura sozinha e lança a regra.
    /// </summary>
    public async Task<LeituraDormente> ConferirPeca(int idDormente, Rfid tagAtiva, LeituraDto dto, int idUsuario, int? idConferencia = null)
    {
        var agora = DateTime.Now;
        var lida = (dto.Tag ?? "").Trim();
        if (lida.Length == 0)
            throw new RegraDeNegocioException("Leia a tag da peça antes de continuar.");
        if (dto.Metodo == MetodoCaptura.Pistola)
            throw new RegraDeNegocioException("A tag é lida pelo módulo do celular; a pistola agora é a NIR, que analisa a madeira.");
        var leitura = new LeituraDormente
        {
            IdDormente = idDormente, IdLeitor = dto.IdLeitor, IdUsuario = idUsuario, MetodoCaptura = dto.Metodo, Leitura = true,
            DataLeitura = agora, TagLida = lida, IdModo = dto.IdModo, PotenciaDbm = dto.PotenciaDbm, RssiDbm = dto.RssiDbm,
            TagsVistas = dto.TagsVistas, IdConferencia = idConferencia, RegistradoEm = agora, RecebidoEm = agora
        };

        if (AlvoUnico.EhAmbigua(dto.Metodo, dto.TagsVistas))
        {
            leitura.IdDormente = null;
            leitura.Ambigua = true;
            await pecas.RegistrarLeitura(leitura);
            throw new RegraDeNegocioException(AlvoUnico.MensagemAmbigua);
        }
        if (lida != tagAtiva.TagRfid)
        {
            var dona = await pecas.BuscarTag(lida);
            leitura.IdDormente = dona?.IdDormente;
            await pecas.RegistrarLeitura(leitura);
            throw new RegraDeNegocioException(dona is null
                ? $"A tag {lida} não está associada a nenhuma peça."
                : $"A tag {lida} é de outra peça, não desta.");
        }
        return leitura;
    }
}