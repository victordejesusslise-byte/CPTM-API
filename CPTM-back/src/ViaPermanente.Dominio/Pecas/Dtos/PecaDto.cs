namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>
/// A peça como a tela mostra. Situacao é calculada: AguardandoVerificacao, Aprovada, Reprovada, NoArmazem, NaVia ou Retirada.
/// Nota é a nota única de estado (Bom, Regular, Ruim, Crítico; nula sem avaliação): veja NotaDaPeca.
/// Codigo é DM-E(empresa)-L(lote)-(peça), como no protótipo.
/// </summary>
public record PecaDto(int Id, string Codigo, string? Tag, string? TagFornecedor, int IdPedido, int IdLote, int IdFeixe, string NumeroFeixe,
    int IdEspecie, string Especie, byte Classe, int IdTamanho, decimal ComprimentoM,
    int IdArmazem, string Armazem, int? IdPilha, string? Pilha, DateTime? EntradaArmazemEm, int? IdTrecho, decimal? KmProjeto, string Situacao,
    string? Nota = null);