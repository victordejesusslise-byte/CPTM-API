using ViaPermanente.Dominio.Models.Laudos;

namespace ViaPermanente.Dominio.Repositories;

/// <summary>Desenha o documento em PDF (A4).</summary>
public interface IDocumentoPdfRepository
{
    byte[] Montar(DocumentoPdf documento);
}