namespace ViaPermanente.Dominio.Apresentacao.Models;

/// <summary>Quantas linhas cada etapa do fluxo tem no banco (de um pedido ou do banco inteiro).</summary>
public record ContagemFluxo(
    int Pedidos, int ItensPedido, int PecasPedidas, int Notas,
    int Recebimentos, int Lotes, int Feixes, int PecasNosFeixes,
    int Dormentes, int TagsAtivas, int TagsDesativadas, int Classificacoes,
    int Aprovadas, int Reprovadas,
    int Movimentacoes, int EmPilha);