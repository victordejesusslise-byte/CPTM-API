using System.Globalization;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Via.Dtos;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Via;

/// <summary>
/// O que o carro mandou, para o operador da via, o técnico e o supervisor: as passagens com o resumo e a passagem aberta
/// com as leituras. Fora de posição usa o mesmo limite da varredura (parâmetro desvio_limite_mm).
/// </summary>
public class PassagemConsultaService(IPassagemConsultaRepository repo, IParametroRepository parametros, PecaService pecas)
{
    public async Task<IReadOnlyList<PassagemDto>> Ultimas(int quantas)
    {
        var limite = await Limite();
        var lista = new List<PassagemDto>();
        foreach (var p in await repo.Ultimas(Math.Clamp(quantas, 1, 120)))
            lista.Add(await Resumo(p, await repo.Leituras(p.IdPassagem), limite));
        return lista;
    }

    public async Task<PassagemDetalheDto?> Buscar(int idPassagem)
    {
        var passagem = await repo.Buscar(idPassagem);
        if (passagem is null) return null;
        var limite = await Limite();
        var leituras = await repo.Leituras(idPassagem);
        var donos = await pecas.PorIds(leituras.Where(l => l.IdDormente is not null).Select(l => l.IdDormente!.Value).Distinct().ToList());
        var detalhe = leituras
            .Select(l => (Leitura: l, Peca: l.IdDormente is int id ? donos.GetValueOrDefault(id) : null))
            .Select(x => new LeituraDaPassagemDto(x.Leitura.IdLeituraVia, x.Leitura.IdDormente, x.Peca?.Codigo,
                x.Leitura.KmMedido, x.Leitura.Leitura, x.Leitura.DesvioMm, x.Leitura.RachaduraSuspeita, Fora(x.Leitura, limite), x.Leitura.Data, x.Peca?.Nota))
            .OrderByDescending(l => !l.Leitura || l.ForaDePosicao || l.RachaduraSuspeita || l.IdDormente is null)
            .ThenBy(l => l.Km)
            .Take(500)
            .ToList();
        return new PassagemDetalheDto(await Resumo(passagem, leituras, limite), passagem.Veiculo, passagem.KmInicio, passagem.KmFim, limite, detalhe);
    }

    private async Task<PassagemDto> Resumo(Passagem p, IReadOnlyList<LeituraVia> leituras, decimal limite)
    {
        var (sensor, imagem, semAlerta) = await repo.Alertas(p.IdPassagem);
        return new PassagemDto(p.IdPassagem, p.Codigo, p.DataInicio, p.DataFim,
            leituras.Count, leituras.Count(l => !l.Leitura), leituras.Count(l => Fora(l, limite)), leituras.Count(l => l.IdDormente is null),
            await repo.MudancasDeFaixa(p.IdPassagem), sensor, imagem, semAlerta);
    }

    private static bool Fora(LeituraVia l, decimal limite) => l.Leitura && l.DesvioMm is decimal d && Math.Abs(d) > limite;

    private async Task<decimal> Limite()
        => await parametros.Buscar(VarreduraService.ChaveLimite) is { } p && decimal.TryParse(p.Valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 40m;
}
