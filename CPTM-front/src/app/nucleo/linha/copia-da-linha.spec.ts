import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { entrarComo } from '../../compartilhado/testes/sessao-de-teste';
import { Rede } from '../fila/rede';
import { CopiaDaLinha, PecaDaLinha } from './copia-da-linha';

const peca = (id: number, situacao = 'NaVia'): PecaDaLinha => ({
  id, codigo: `DM-E01-L08-00${id}`, tag: `BR00000000${id}`, km: 213.1, posicao: null, idTrecho: 4, situacao, nota: null, ultimaInspecao: null, ultimaInspecaoEm: null,
});

describe('CopiaDaLinha (a cópia das linhas no celular)', () => {
  const abrir = () => {
    localStorage.clear();
    entrarComo('Tecnico');
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Rede, useValue: { online: signal(true) } }] });
    return { copia: TestBed.inject(CopiaDaLinha), http: TestBed.inject(HttpTestingController) };
  };
  const pedido = (http: HttpTestingController): Promise<TestRequest> =>
    vi.waitFor(() => {
      const r = http.match((q) => q.url.endsWith('/campo/linha'));
      if (!r.length) throw new Error('esperando a página');
      return r[0];
    });

  it('a primeira vez baixa tudo em páginas; depois, só o que mudou, e apaga a que saiu da via', async () => {
    const { copia, http } = abrir();
    const primeira = copia.sincronizar();
    const p1 = await pedido(http);
    expect(p1.request.params.get('desde')).toBeNull();
    p1.flush({ pecas: [peca(10), peca(11)], proximo: 11, geradoEm: '2026-10-25T22:00:00' });
    const p2 = await pedido(http);
    expect(p2.request.params.get('depoisDe')).toBe('11');
    p2.flush({ pecas: [peca(12)], proximo: null, geradoEm: '2026-10-25T22:00:01' });
    await primeira;
    expect(copia.total()).toBe(3);
    expect((await copia.pecaPorTag(' BR0000000011 '))?.codigo).toBe('DM-E01-L08-0011');

    const segunda = copia.sincronizar();
    const p3 = await pedido(http);
    expect(p3.request.params.get('desde')).toBe('2026-10-25T22:00:00');
    p3.flush({ pecas: [peca(11, 'Retirada')], proximo: null, geradoEm: '2026-10-25T23:00:00' });
    await segunda;
    expect(copia.total()).toBe(2);
    expect(await copia.pecaPorTag('BR0000000011')).toBeNull();
    expect((await copia.pecaPorCodigo('dm-e01-l08-0012'))?.id).toBe(12);
  });

  it('lista as peças de um trecho pelo km (o mapa do trecho sem rede)', async () => {
    const { copia, http } = abrir();
    const baixou = copia.sincronizar();
    (await pedido(http)).flush({
      pecas: [{ ...peca(21), km: 213.4 }, { ...peca(22), km: 213.2 }, { ...peca(23), idTrecho: 5 }], proximo: null, geradoEm: '2026-10-26T20:00:00',
    });
    await baixou;
    expect((await copia.pecasDoTrecho(4)).map((p) => p.id)).toEqual([22, 21]);
    expect((await copia.pecasDoTrecho(9)).length).toBe(0);
  });
});
