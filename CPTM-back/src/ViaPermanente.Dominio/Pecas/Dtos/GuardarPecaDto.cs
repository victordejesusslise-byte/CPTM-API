using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>A entrada da peça aprovada no armazém, depois de lida. Sem pilha, vale a sugerida (tamanho e classe da peça).</summary>
public class GuardarPecaDto
{
    [Required] public LeituraDto Leitura { get; init; } = new();
    public int? IdPilha { get; init; }
    public Guid? IdCliente { get; init; }
    public DateTime? RegistradoEm { get; init; }
}