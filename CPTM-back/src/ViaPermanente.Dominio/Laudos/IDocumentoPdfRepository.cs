using ViaPermanente.Dominio.Laudos.Models;

namespace ViaPermanente.Dominio.Laudos;

/// <summary>Desenha o documento em PDF (A4).</summary>
public interface IDocumentoPdfRepository
{
    byte[] Montar(DocumentoPdf documento);
}