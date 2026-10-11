using ViaPermanente.Dominio.Models.Compras;

namespace ViaPermanente.Dominio.Dtos.Cadastros;

public record EmpresaDto(int Id, string Nome, string? Cnpj, string Papel, bool Ativo);
public record ContratoDto(int Id, int IdEmpresa, string Empresa, string NumeroAta, string NumeroContrato, DateOnly? VigenciaInicio, DateOnly? VigenciaFim);
public record EstacaoDto(int Id, string Nome, string Linha, decimal Km, int Ordem);
public record TrechoDto(int Id, int IdEstacaoInicio, string EstacaoInicio, int IdEstacaoFim, string EstacaoFim, byte ClasseExigida);

public class NovaEmpresaDto
{
    public string Nome { get; init; } = "";
    public string? Cnpj { get; init; }
    public string? Endereco { get; init; }
    public string? Telefone { get; init; }
    public PapelEmpresa Papel { get; init; }
}

public class NovoContratoDto
{
    public int IdEmpresa { get; init; }
    public string NumeroAta { get; init; } = "";
    public string NumeroContrato { get; init; } = "";
    public DateOnly? VigenciaInicio { get; init; }
    public DateOnly? VigenciaFim { get; init; }
}

public class NovaEstacaoDto
{
    public string Nome { get; init; } = "";
    public string Linha { get; init; } = "9-Esmeralda";
    public decimal Km { get; init; }
    public int Ordem { get; init; }
}

/// <summary>O trecho entre duas estações vizinhas na ordem da linha; o par é único.</summary>
public class NovoTrechoDto
{
    public int IdEstacaoInicio { get; init; }
    public int IdEstacaoFim { get; init; }
    public byte ClasseExigida { get; init; }
}