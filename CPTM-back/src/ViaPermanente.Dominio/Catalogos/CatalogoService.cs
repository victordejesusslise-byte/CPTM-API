using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Catalogos.Dtos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Catalogos;

/// <summary>Os catálogos que as telas usam: tamanhos (com o m³), espécies e armazéns com as pilhas.</summary>
public class CatalogoService(ICatalogoRepository catalogo)
{
    public async Task<IReadOnlyList<TamanhoDto>> ListarTamanhos()
        => (await catalogo.ListarTamanhos())
            .Where(t => t.Ativo)
            .Select(t => new TamanhoDto(t.IdTamanho, t.ComprimentoM, t.LarguraM, t.AlturaM,
                VolumeHelper.M3(t.ComprimentoM, t.LarguraM, t.AlturaM), t.MedidaProvisoria))
            .ToList();

    public async Task<IReadOnlyList<EspecieDto>> ListarEspecies()
        => (await catalogo.ListarEspecies())
            .Where(e => e.Ativo)
            .Select(e => new EspecieDto(e.IdEspecie, e.NomeCientifico, e.Familia, e.NomePopular, e.Classe))
            .ToList();

    public async Task<IReadOnlyList<ModoLeituraDto>> ListarModos()
        => (await catalogo.ListarModos())
            .Select(m => new ModoLeituraDto(m.IdModo, m.Nome, m.PotenciaDbm, m.FolgaRssiDb, m.Antena.ToString(), m.ExigeAlvoUnico))
            .ToList();

    public async Task<IReadOnlyList<LeitorDto>> ListarLeitores()
    {
        var tipos = (await catalogo.ListarTiposLeitor()).ToDictionary(t => t.IdTipoLeitor, t => t.Nome);
        return (await catalogo.ListarLeitores())
            .Select(l => new LeitorDto(l.IdLeitor, l.Codigo, l.IdTipoLeitor, tipos.GetValueOrDefault(l.IdTipoLeitor, ""), l.NumeroSerie, l.Ativo))
            .ToList();
    }

    /// <summary>Cadastra um leitor (pistola, módulo do celular ou carro). Código e número de série são únicos.</summary>
    public async Task<LeitorDto> RegistrarLeitor(NovoLeitorDto novo)
    {
        var codigo = (novo.Codigo ?? "").Trim();
        var serie = string.IsNullOrWhiteSpace(novo.NumeroSerie) ? null : novo.NumeroSerie.Trim();
        if (codigo.Length == 0)
            throw new RegraDeNegocioException("Informe o código do leitor (ex.: MOD-03).");
        if ((await catalogo.ListarTiposLeitor()).All(t => t.IdTipoLeitor != novo.IdTipoLeitor))
            throw new RegraDeNegocioException("Tipo de leitor não encontrado.");
        var leitores = await catalogo.ListarLeitores();
        if (leitores.Any(l => string.Equals(l.Codigo, codigo, StringComparison.OrdinalIgnoreCase)))
            throw new RegraDeNegocioException($"Já existe o leitor {codigo}.");
        if (serie is not null && leitores.Any(l => l.NumeroSerie == serie))
            throw new RegraDeNegocioException($"Já existe um leitor com o número de série {serie}.");

        var leitor = new Leitor { IdTipoLeitor = novo.IdTipoLeitor, Codigo = codigo, NumeroSerie = serie, IdSetor = novo.IdSetor, Ativo = true };
        await catalogo.AdicionarLeitor(leitor);
        return (await ListarLeitores()).First(l => l.Id == leitor.IdLeitor);
    }

    /// <summary>Os armazéns ativos, do km menor para o maior, cada um com as pilhas e as peças guardadas.</summary>
    public async Task<IReadOnlyList<ArmazemDto>> ListarArmazens()
    {
        var comprimentos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho, t => t.ComprimentoM);
        var pecas = await catalogo.ContarPecasPorPilha();
        var pilhas = (await catalogo.ListarPilhas()).Where(p => p.Ativo).ToLookup(p => p.IdArmazem);

        return (await catalogo.ListarArmazens())
            .Where(a => a.Ativo)
            .Select(a =>
            {
                var dele = pilhas[a.IdArmazem]
                    .Select(p => new PilhaDto(p.IdPilha, p.Codigo, p.Setor, comprimentos.GetValueOrDefault(p.IdTamanho),
                        p.Classe, pecas.GetValueOrDefault(p.IdPilha)))
                    .ToList();
                return new ArmazemDto(a.IdArmazem, a.Nome, a.Linha, a.Km, dele.Sum(p => p.Pecas), dele);
            })
            .ToList();
    }
}