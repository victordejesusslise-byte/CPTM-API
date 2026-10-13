using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Armazens;

public interface IArmazemRepository
{
    Task<ConferenciaArmazem?> BuscarConferencia(int idConferencia);
    Task<ConferenciaArmazem?> ConferenciaAberta(int idArmazem);
    Task CriarConferencia(ConferenciaArmazem conferencia);
    Task ConcluirConferencia(ConferenciaArmazem conferencia);
    Task<IReadOnlyList<LeituraDormente>> LeiturasDaConferencia(int idConferencia);
    Task RegistrarLeituras(IReadOnlyList<LeituraDormente> leituras);
    /// <summary>As peças que o sistema diz estarem nas pilhas do armazém (id da peça, pilha, tag ativa).</summary>
    Task<IReadOnlyList<(int IdDormente, int IdPilha, string? Tag)>> PecasNoArmazem(int idArmazem);
    /// <summary>A peça dona de cada tag ativa da lista.</summary>
    Task<IReadOnlyDictionary<string, int>> DonosDasTags(IReadOnlyCollection<string> tags);
    Task TrocarPilha(int idDormente, int idPilhaNova, Movimentacao movimentacao, LeituraDormente leitura);
    /// <summary>Leituras avisadas em campo cuja tag ainda não tem peça.</summary>
    Task<IReadOnlyList<LeituraDormente>> AvisosPendentes();
    Task Vincular(Rfid nova, int? idRfidAntiga, LeituraDormente leitura);
}