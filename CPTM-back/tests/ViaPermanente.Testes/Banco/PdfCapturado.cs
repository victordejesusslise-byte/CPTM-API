using ViaPermanente.Dados.Laudos;
using ViaPermanente.Dominio.Laudos;
using ViaPermanente.Dominio.Laudos.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>Desenha o PDF de verdade e guarda o conteúdo montado, para o teste conferir o que vai no laudo.</summary>
internal class PdfCapturado : IDocumentoPdfRepository
{
    public DocumentoPdf? Ultimo { get; private set; }
    public byte[] Montar(DocumentoPdf documento) { Ultimo = documento; return new DocumentoPdfRepository().Montar(documento); }
    public string Texto => Ultimo is null ? "" : string.Join(" | ", Ultimo.Secoes.SelectMany(s => s.Campos.Select(c => $"{c.Rotulo}: {c.Valor}")
        .Concat(s.Tabela?.Linhas.SelectMany(l => l) ?? [])));
}