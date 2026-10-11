namespace ViaPermanente.Dominio.Models.Compras;

/// <summary>EMPRESA: fornecedor, tratadora ou inspetora.</summary>
public class Empresa
{
    public int IdEmpresa { get; set; }
    public string Nome { get; set; } = "";
    public string? Cnpj { get; set; }
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }
    public bool Ativo { get; set; } = true;
    public PapelEmpresa Papel { get; set; }
}