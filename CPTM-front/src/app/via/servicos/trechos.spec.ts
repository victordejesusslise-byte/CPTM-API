import { PecaNoTrechoDto, faixasDoTrecho } from './trechos';

const peca = (id: number, km: number, nota: string | null): PecaNoTrechoDto => ({ id, codigo: `DM-${id}`, tag: null, posicao: null, km, nota });

describe('faixasDoTrecho (o mapa do trecho montado no celular, sem rede)', () => {
  it('corta em faixas de 100 m, conta as notas e estica o fim com a peça além do km da estação', () => {
    const faixas = faixasDoTrecho(10, 10.3, 100, [peca(1, 10.05, 'Bom'), peca(2, 10.01, 'Crítico'), peca(3, 10.25, null), peca(4, 10.35, 'Ruim')]);
    expect(faixas.map((f) => [f.kmInicio, f.kmFim])).toEqual([[10, 10.1], [10.1, 10.2], [10.2, 10.3], [10.3, 10.4]]);
    expect(faixas[0]).toMatchObject({ bom: 1, critico: 1, regular: 0, ruim: 0, semNota: 0 });
    expect(faixas[0].pecas.map((p) => p.id)).toEqual([2, 1]);
    expect(faixas[1].pecas).toEqual([]);
    expect(faixas[2].semNota).toBe(1);
    expect(faixas[3].ruim).toBe(1);
  });

  it('sem peça, uma faixa pelo menos', () => {
    expect(faixasDoTrecho(5, 5, 100, [])).toHaveLength(1);
  });
});
