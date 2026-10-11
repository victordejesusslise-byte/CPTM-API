namespace ViaPermanente.Dominio.Models.Patio;

/// <summary>CONFERENCIA_ARMAZEM: os números são guardados ao concluir, porque o estoque muda depois.</summary>
public class ConferenciaArmazem : IRegistroDeCampo
{
    public int IdConferencia { get; set; }
    public int IdArmazem { get; set; }
    public int IdUsuario { get; set; }
    public int? IdLeitor { get; set; }
    public DateTime IniciadaEm { get; set; }
    public DateTime? ConcluidaEm { get; set; }
    public int? Esperadas { get; set; }
    public int? Lidas { get; set; }
    public int? Faltando { get; set; }
    public int? AMais { get; set; }
    public string? Observacao { get; set; }
    public Guid? IdCliente { get; set; }
    public DateTime RegistradoEm { get; set; }
    public DateTime? RecebidoEm { get; set; }
}