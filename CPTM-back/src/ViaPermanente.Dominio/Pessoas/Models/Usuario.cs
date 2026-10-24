namespace ViaPermanente.Dominio.Pessoas.Models;

/// <summary>
/// USUARIO: quem entra no sistema, com o nome e o e-mail (o login, único) e a própria senha, guardada só em hash.
/// Desde 2026-10-23 a PESSOA está aqui dentro (Victor: "unir pessoa e usuário"; só o usuário usava a pessoa).
/// </summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public Cargo TipoCargo { get; set; }
    public DateTime DataCadastro { get; set; }
    public string SenhaHash { get; set; } = "";
    public bool Ativo { get; set; } = true;
    /// <summary>Vai no token; sobe ao sair, ao desativar ou ao trocar a senha, e todo token antigo deixa de valer na hora (segurança, 2026-10-18).</summary>
    public int VersaoSessao { get; set; }
    /// <summary>Senhas erradas seguidas; volta a zero no login certo.</summary>
    public int TentativasFalhas { get; set; }
    /// <summary>Depois de 5 senhas erradas seguidas, a conta espera até aqui (30 s, depois 1 min, depois 5 min).</summary>
    public DateTime? EsperarAte { get; set; }
}
