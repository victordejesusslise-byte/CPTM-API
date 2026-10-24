namespace ViaPermanente.Dominio.Pedidos.Models;

/// <summary>EMPRESA: fornecedor, tratadora ou inspetora.</summary>
public class Empresa
{
    public int IdEmpresa { get; set; }
    public string Nome { get; set; } = "";
    public string? Cnpj { get; set; }
    public bool Ativo { get; set; } = true;
    public PapelEmpresa Papel { get; set; }
}