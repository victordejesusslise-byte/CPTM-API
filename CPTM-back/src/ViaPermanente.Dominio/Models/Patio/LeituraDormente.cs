namespace ViaPermanente.Dominio.Models.Patio;

/// <summary>LEITURA_DORMENTE: toda leitura com o módulo é gravada, também a ambígua (sem peça).</summary>
public class LeituraDormente : IRegistroDeCampo
{
    public int IdLeitura { get; set; }
    /// <summary>Nulo: tag sem cadastro, achada em campo.</summary>
    public int? IdDormente { get; set; }
    /// <summary>Nulo se foi digitado.</summary>
    public int? IdLeitor { get; set; }
    public int IdUsuario { get; set; }
    public MetodoCaptura MetodoCaptura { get; set; }
    public bool Leitura { get; set; }
    public DateTime DataLeitura { get; set; }
    public decimal? Umidade { get; set; }
    public decimal? Temperatura { get; set; }
    public string? Observacao { get; set; }
    public string? TagLida { get; set; }
    public int? IdModo { get; set; }
    public decimal? PotenciaDbm { get; set; }
    public decimal? RssiDbm { get; set; }
    public int? TagsVistas { get; set; }
    public bool Ambigua { get; set; }
    public int? IdConferencia { get; set; }
    /// <summary>Na conferência do armazém: a pilha onde a peça respondeu (desde 2026-10-11).</summary>
    public int? IdPilha { get; set; }
    public decimal? Km { get; set; }
    public bool Aviso { get; set; }
    public Guid? IdCliente { get; set; }
    public DateTime RegistradoEm { get; set; }
    public DateTime? RecebidoEm { get; set; }
}