
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>Como a peça foi identificada: separa tag quebrada de pistola sem bateria e de peça digitada.</summary>
public enum MetodoCaptura
{
    /// <summary>Leitor de tag antigo: fica só para o histórico. Desde 2026-10-13 só o módulo lê a tag e a pistola é a NIR.</summary>
    Pistola,
    /// <summary>O módulo RFID ligado ao celular: o único que lê e grava a nossa tag.</summary>
    ModuloUhf,
    /// <summary>Pela câmera do celular.</summary>
    Camera,
    /// <summary>Por NFC, encostando o celular.</summary>
    Nfc,
    /// <summary>Digitado à mão.</summary>
    Digitado
}
