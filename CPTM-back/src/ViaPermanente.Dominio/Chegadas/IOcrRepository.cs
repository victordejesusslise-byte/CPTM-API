using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Dominio.Chegadas;

/// <summary>O motor de OCR (Tesseract, no servidor). Lê a mesma foto de mais de um jeito: uma leitura por jeito, da mais certeira para a menos.</summary>
public interface IOcrRepository
{
    IReadOnlyList<ResultadoOcr> Ler(byte[] imagem, string modeloBase);
}