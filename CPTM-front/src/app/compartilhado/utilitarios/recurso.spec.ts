import { Injector, runInInjectionContext } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { valorDe } from './recurso';

describe('valorDe (ler a busca sem travar a tela)', () => {
  const buscar = <T>(fonte: () => import('rxjs').Observable<T>) =>
    runInInjectionContext(TestBed.inject(Injector), () => rxResource({ stream: fonte }));

  it('com valor, devolve o valor', async () => {
    const r = buscar(() => of([1, 2]));
    await vi.waitFor(() => expect(r.hasValue()).toBe(true));
    expect(valorDe(r)).toEqual([1, 2]);
  });

  it('com erro, devolve undefined em vez de lançar (Victor, 2026-10-31: as telas ficavam em "Carregando…")', async () => {
    const r = buscar(() => throwError(() => new Error('403')));
    await vi.waitFor(() => expect(r.error()).toBeTruthy());
    expect(() => r.value()).toThrow();
    expect(valorDe(r)).toBeUndefined();
  });
});
