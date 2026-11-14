namespace ViaPermanente.Dominio.Pedidos.Models;

/// <summary>EMPRESA: fornecedor, tratadora ou inspetora.</summary>
public class Empresa
{
    public int IdEmpresa { get; set; }
    public string Nome { get; set; } = "";
    /// <summary>A sigla (TG), de 2 a 6 letras ou números em maiúsculas: começa a tag do fornecedor (TG-20261013193233-28). Opcional.</summary>
    public string? Sigla { get; set; }
    public string? Cnpj { get; set; }
    public bool Ativo { get; set; } = true;
    public PapelEmpresa Papel { get; set; }
}