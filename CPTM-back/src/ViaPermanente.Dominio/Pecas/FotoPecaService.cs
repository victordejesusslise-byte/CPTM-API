using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Laudos;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Pecas;

/// <summary>
/// A foto do dormente na entrada (pedido do Victor em 2026-10-12): guardada na pasta das fotos, fora do banco, com o ID
/// da peça no caminho; a verificação de qualidade grava esse caminho. Depois, na via, o quadro da câmera do carro mostra
/// "como ficou", e a IA (modelo de imagem) compara os dois quando existir.
/// </summary>
public class FotoPecaService(IFotoRepository fotos, IFotoPecaRepository repo)
{
    private static readonly string[] Extensoes = [".jpg", ".jpeg", ".png", ".webp"];
    public const int TamanhoMaximo = 15_000_000;

    public async Task<string> SalvarEntrada(int idDormente, byte[] conteudo, string extensao)
    {
        if (!await repo.PecaExiste(idDormente)) throw new RegraDeNegocioException("Peça não encontrada.");
        if (conteudo.Length == 0) throw new RegraDeNegocioException("A foto veio vazia.");
        if (conteudo.Length > TamanhoMaximo) throw new RegraDeNegocioException("A foto passa de 15 MB.");
        var ext = (extensao ?? "").ToLowerInvariant();
        if (!Extensoes.Contains(ext)) throw new RegraDeNegocioException("Mande a foto em JPG, PNG ou WEBP.");
        return fotos.Salvar(conteudo, $"pecas/{idDormente}/entrada", ext);
    }

    public async Task<FotosDaPecaDto?> DaPeca(int idDormente)
    {
        if (!await repo.PecaExiste(idDormente)) return null;
        var entrada = (await repo.DaEntrada(idDormente))
            .Select(f => new FotoDaPecaDto("entrada", f.Arquivo, f.Data, $"Verificação de qualidade: {f.Resultado.ToLowerInvariant()}", fotos.Ler(f.Arquivo) is not null, null)).ToList();
        var via = (await repo.DaCamera(idDormente))
            .Select(f => new FotoDaPecaDto("via", f.Arquivo, f.Data, $"Câmera do carro: {f.Defeito.ToLowerInvariant()} grau {f.Grau}, confiança {f.Confianca:P0}", fotos.Ler(f.Arquivo) is not null, null)).ToList();
        return new FotosDaPecaDto(idDormente, entrada, via);
    }

    /// <summary>O arquivo de uma foto que está no banco (verificação ou câmera); nulo se não está no servidor.</summary>
    public byte[]? Arquivo(string caminho) => fotos.Ler(caminho);
}
