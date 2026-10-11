using ViaPermanente.Dominio.Models.Chegada;

namespace ViaPermanente.Dominio.Repositories;

/// <summary>O motor de OCR (Tesseract, no servidor).</summary>
public interface IOcrRepository
{
    ResultadoOcr Ler(byte[] imagem, string modeloBase);
}