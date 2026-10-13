using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Dominio.Chegadas;

/// <summary>O motor de OCR (Tesseract, no servidor).</summary>
public interface IOcrRepository
{
    ResultadoOcr Ler(byte[] imagem, string modeloBase);
}