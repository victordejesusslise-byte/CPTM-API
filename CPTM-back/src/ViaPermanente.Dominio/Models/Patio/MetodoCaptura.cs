namespace ViaPermanente.Dominio.Models.Patio;

/// <summary>Como a peça foi identificada: separa tag quebrada de pistola sem bateria e de peça digitada.</summary>
public enum MetodoCaptura
{
    Pistola,
    ModuloUhf,
    Camera,
    Nfc,
    Digitado
}
