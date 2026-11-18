using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Armazens.Dtos;

/// <summary>
/// Guardar o lote inteiro (Victor, 2026-11-17): a tag lida acha o lote e cada grupo (tamanho e classe) vai para a pilha escolhida,
/// todas no mesmo armazém. Entram só as aprovadas que ainda não estão numa pilha; a tag de cada peça não é lida.
/// </summary>
public class GuardarLoteDto
{
    /// <summary>Id do armazém do lote todo (→ ARMAZEM); toda pilha escolhida tem que ser dele.</summary>
    [Range(1, int.MaxValue)] public int IdArmazem { get; init; }
    /// <summary>A leitura da tag que achou o lote; ela fica na movimentação da peça lida.</summary>
    [Required] public LeituraDto Leitura { get; init; } = new();
    /// <summary>A pilha de cada grupo do lote.</summary>
    [MinLength(1)] public List<GrupoParaGuardarDto> Grupos { get; init; } = [];
    /// <summary>A hora do celular; sem ela, vale a do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}

/// <summary>Um grupo do lote e a pilha escolhida para ele.</summary>
public class GrupoParaGuardarDto
{
    /// <summary>Id do tamanho do grupo (→ TAMANHO_DORMENTE).</summary>
    [Range(1, int.MaxValue)] public int IdTamanho { get; init; }
    /// <summary>A classe do grupo: 1 ou 2.</summary>
    [Range(1, 2)] public byte Classe { get; init; }
    /// <summary>Id da pilha escolhida (→ PILHA): do tamanho e da classe do grupo, no armazém do lote.</summary>
    [Range(1, int.MaxValue)] public int IdPilha { get; init; }
}

/// <summary>O lote da peça lida, para guardar tudo de uma vez: as peças no pátio, separadas por tamanho e classe.</summary>
/// <param name="IdLote">Id do lote (→ LOTE).</param>
/// <param name="CodigoLote">O código do lote: L- e o código do pedido, ex.: L-PED-0012.</param>
/// <param name="IdDormenteLido">Id da peça da tag lida (→ DORMENTE).</param>
/// <param name="CodigoLido">Código impresso da peça lida: DM-E(empresa)-L(lote)-(id da peça).</param>
/// <param name="IdArmazem">Id do armazém onde a peça lida chegou (→ ARMAZEM): o sugerido para o lote todo.</param>
/// <param name="Pecas">Quantas peças do lote estão no pátio (fora da via).</param>
/// <param name="Grupos">As peças no pátio, por tamanho e classe.</param>
public record LoteParaGuardarDto(int IdLote, string CodigoLote, int IdDormenteLido, string? CodigoLido, int IdArmazem, int Pecas, IReadOnlyList<GrupoDoLoteDto> Grupos);

/// <summary>Um grupo do lote: as peças de um tamanho e uma classe.</summary>
/// <param name="IdTamanho">Id do tamanho (→ TAMANHO_DORMENTE).</param>
/// <param name="ComprimentoM">O comprimento, em metros.</param>
/// <param name="Classe">A classe: 1 ou 2.</param>
/// <param name="AGuardar">Aprovadas na verificação e ainda sem pilha: as que o botão guarda.</param>
/// <param name="JaNaPilha">As que já estão numa pilha.</param>
/// <param name="NaoAprovadas">Reprovadas ou ainda não verificadas: não entram no armazém.</param>
public record GrupoDoLoteDto(int IdTamanho, decimal ComprimentoM, byte Classe, int AGuardar, int JaNaPilha, int NaoAprovadas);
