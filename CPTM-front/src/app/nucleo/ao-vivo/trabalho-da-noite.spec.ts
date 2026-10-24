import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TrocaDto } from '../../campo/entidades/campo';
import { PecaDto } from '../../pecas/entidades/peca';
import { GUARDADO } from '../fila/guardado';
import { Rede } from '../fila/rede';
import { NO_APP, PlanoCampoDto, TrabalhoDaNoite, versaoMaisNova } from './trabalho-da-noite';

describe('TrabalhoDaNoite (o plano guardado no celular)', () => {
  const online = signal(true);
  const abrir = (noApp = true) => {
    localStorage.clear(); // sem ninguém logado: não liga o ao vivo nos testes
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Rede, useValue: { online } }, { provide: NO_APP, useValue: noApp }],
    });
    return { trabalho: TestBed.inject(TrabalhoDaNoite), http: TestBed.inject(HttpTestingController) };
  };
  const plano: PlanoCampoDto = {
    noite: '2026-10-24', geradoEm: '2026-10-24T18:00:00',
    trocas: [{ id: 5, status: 'Separada' } as TrocaDto, { id: 6, status: 'Aprovada' } as TrocaDto],
    alertas: [],
    pecas: [{ id: 2, tag: 'BR0000000002', codigo: 'DM-1' } as PecaDto],
  };

  beforeEach(() => online.set(true));

  it('compara as versões número a número', () => {
    expect(versaoMaisNova('0.1.12', '0.1.9')).toBe(true);
    expect(versaoMaisNova('0.1.9', '0.1.9')).toBe(false);
    expect(versaoMaisNova('0.1.8', '0.1.10')).toBe(false);
  });

  it('baixa o plano, guarda no celular e as telas leem dele', async () => {
    const { trabalho, http } = abrir();
    const baixou = trabalho.atualizar();
    http.expectOne('http://localhost:8045/api/campo/plano').flush(plano);
    await baixou;
    expect(trabalho.trocas(['Separada']).map((t) => t.id)).toEqual([5]);
    expect(trabalho.troca(6)?.status).toBe('Aprovada');
    expect(trabalho.pecaPorTag(' BR0000000002 ')?.codigo).toBe('DM-1');
    expect(JSON.parse((await TestBed.inject(GUARDADO).ler('ybyra.plano'))!).noite).toBe('2026-10-24');
  });

  it('sem rede, no app, usa o guardado; no site do navegador, não', () => {
    online.set(false);
    expect(abrir(true).trabalho.usarGuardado()).toBe(true);
    TestBed.resetTestingModule();
    expect(abrir(false).trabalho.usarGuardado()).toBe(false);
  });
});
