import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AlertaDto, TrocaDto } from '../../campo/entidades/campo';
import { PecaDto } from '../../pecas/entidades/peca';
import { GUARDADO } from '../fila/guardado';
import { Rede } from '../fila/rede';
import { Sessao } from '../sessao/sessao';
import { NO_APP, PacoteDoOperadorDto, PlanoCampoDto, TrabalhoDaNoite, versaoMaisNova } from './trabalho-da-noite';

describe('TrabalhoDaNoite (o plano guardado no celular)', () => {
  const online = signal(true);
  // daVia: quem trabalha na via (recebe o pacote do operador), sem ninguém logado de verdade, para não ligar o ao vivo nos testes.
  const abrir = (noApp = true, daVia = false) => {
    localStorage.clear(); // sem ninguém logado: não liga o ao vivo nos testes
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(), provideHttpClientTesting(), { provide: Rede, useValue: { online } }, { provide: NO_APP, useValue: noApp },
        ...(daVia ? [{ provide: Sessao, useValue: { usuario: signal(null), token: () => null, temCargo: () => true } }] : []),
      ],
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

  it('para quem trabalha na via, guarda o pacote do operador: sem rede, aparecem as feitas e os alertas decididos dos últimos dias', async () => {
    const { trabalho, http } = abrir(true, true);
    const pacote: PacoteDoOperadorDto = {
      dias: 7, geradoEm: '2026-10-24T17:00:00', trechos: [], passagens: [],
      trocas: [{ id: 5, status: 'Separada' } as TrocaDto, { id: 9, status: 'Feita' } as TrocaDto],
      alertas: [{ id: 3, origem: 'Sensor', status: 'Descartado' } as AlertaDto, { id: 4, origem: 'Sensor', status: 'Aberto' } as AlertaDto],
    };
    const baixou = trabalho.atualizar();
    http.expectOne('http://localhost:8045/api/campo/plano').flush(plano);
    (await vi.waitFor(() => http.expectOne('http://localhost:8045/api/campo/operador?dias=7'))).flush(pacote);
    await baixou;
    expect(trabalho.trocas(['Separada']).map((t) => t.id)).toEqual([5]); // a do plano, sem repetir a do pacote
    expect(trabalho.trocas(['Feita']).map((t) => t.id)).toEqual([9]);
    expect(trabalho.alertas('Sensor').map((a) => a.id)).toEqual([3, 4]);
    expect(trabalho.alertas('Sensor', 'Aberto').map((a) => a.id)).toEqual([4]);
    expect(trabalho.atualizadoEm()).toBe('2026-10-24T17:00:00'); // o mais velho entre o plano e o pacote
    expect(JSON.parse((await TestBed.inject(GUARDADO).ler('ybyra.operador'))!).dias).toBe(7);

    const trocou = trabalho.escolherDias(30);
    (await vi.waitFor(() => http.expectOne('http://localhost:8045/api/campo/operador?dias=30'))).flush({ ...pacote, dias: 30 });
    await trocou;
    expect(trabalho.pacote()?.dias).toBe(30);
    expect(await TestBed.inject(GUARDADO).ler('ybyra.dias')).toBe('30');
  });

  it('quem não trabalha na via não baixa o pacote', async () => {
    const { trabalho, http } = abrir();
    const baixou = trabalho.atualizar();
    http.expectOne('http://localhost:8045/api/campo/plano').flush(plano);
    await baixou;
    http.expectNone((q) => q.url.endsWith('/campo/operador'));
    expect(trabalho.pacote()).toBeNull();
  });
});
