using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>A entrada da peça aprovada no armazém, depois de lida. Sem pilha, vale a sugerida (tamanho e classe da peça).</summary>
public class GuardarPecaDto
{
    /// <summary>A leitura da tag da peça, com uma tag só no alcance.</summary>
    [Required] public LeituraDto Leitura { get; init; } = new();
    /// <summary>Id da pilha escolhida (→ PILHA). Nulo: vale a pilha sugerida.</summary>
    public int? IdPilha { get; init; }
    /// <summary>Gerado no celular: o mesmo envio repetido entra uma vez.</summary>
    public Guid? IdCliente { get; init; }
    /// <summary>A hora do celular; sem ela, vale a do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}