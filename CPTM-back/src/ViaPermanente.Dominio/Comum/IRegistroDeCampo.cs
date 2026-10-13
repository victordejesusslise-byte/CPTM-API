namespace ViaPermanente.Dominio.Comum;

/// <summary>
/// Registro que o celular pode gravar sem internet e enviar depois.
/// O IdCliente é gerado no celular e é único: o mesmo registro enviado duas vezes entra uma vez.
/// </summary>
public interface IRegistroDeCampo
{
    Guid? IdCliente { get; set; }
    DateTime RegistradoEm { get; set; }
    DateTime? RecebidoEm { get; set; }
}