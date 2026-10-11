namespace ViaPermanente.Api.Models;

/// <summary>Seção "Jwt" da configuração. A Chave vem do .env (Jwt__Chave), nunca do appsettings.</summary>
public class JwtOpcoes
{
    public string Chave { get; set; } = "";
    public string Emissor { get; set; } = "";
    public string Publico { get; set; } = "";
    public int MinutosValidade { get; set; } = 480;
}