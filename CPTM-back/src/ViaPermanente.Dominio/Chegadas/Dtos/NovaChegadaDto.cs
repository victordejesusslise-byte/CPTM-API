using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Chegadas.Dtos;

/// <summary>A chegada da carga de um pedido num armazém.</summary>
public class NovaChegadaDto
{
    public int? IdNota { get; init; }
    [Range(1, int.MaxValue)] public int IdArmazem { get; init; }
    /// <summary>Provavelmente DOF; genérico até confirmar (Documento de origem do IBAMA).</summary>
    public string? DocumentoOrigemTipo { get; init; }
    public string? DocumentoOrigemNumero { get; init; }
    /// <summary>Sem data, vale agora.</summary>
    public DateTime? DataRecebimento { get; init; }
    /// <summary>Quantas peças o operador contou.</summary>
    [Range(1, 100000)] public int QuantidadeRecebida { get; init; }
    public string? Observacao { get; init; }
    [Required] public List<NovoFeixeDto> Feixes { get; init; } = [];
}