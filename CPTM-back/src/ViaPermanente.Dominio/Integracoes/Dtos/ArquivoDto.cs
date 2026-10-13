namespace ViaPermanente.Dominio.Integracoes.Dtos;

/// <summary>Um arquivo pronto para baixar.</summary>
public record ArquivoDto(string Nome, byte[] Conteudo)
{
    public const string TipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}