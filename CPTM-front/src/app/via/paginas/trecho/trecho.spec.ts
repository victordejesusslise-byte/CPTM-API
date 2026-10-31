import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Trecho } from './trecho';
import { MapaDoTrechoDto, TrechoDoMapaDto } from '../../servicos/trechos';

const trecho: TrechoDoMapaDto = { id: 7, nome: 'Estação A → Estação B', linha: '9-Esmeralda', kmInicio: 213, kmFim: 213.2, pecas: 2, ruimOuCritico: 1 };
const mapa: MapaDoTrechoDto = {
  trecho, faixaM: 100,
  faixas: [
    { kmInicio: 213, kmFim: 213.1, bom: 1, regular: 0, ruim: 0, critico: 0, semNota: 0,
      pecas: [{ id: 1, codigo: 'DM-E01-L04-0039', tag: 'BR0000000039', posicao: 'L09-213000-0001', km: 213, nota: 'Bom' }] },
    { kmInicio: 213.1, kmFim: 213.2, bom: 0, regular: 0, ruim: 1, critico: 0, semNota: 0,
      pecas: [{ id: 2, codigo: 'DM-E01-L04-0040', tag: 'BR0000000040', posicao: 'L09-213106-0002', km: 213.106, nota: 'Ruim' }] },
  ],
};

describe('Trecho', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] }));

  it('abre o trecho com peça, pinta as faixas com peça na via e abre a faixa com peça ruim, com a lupa na peça ruim', async () => {
    const fixture = TestBed.createComponent(Trecho);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne((r) => r.url.endsWith('/via/trechos')).flush([trecho]);
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();
    await new Promise((r) => setTimeout(r));
    http.expectOne((r) => r.url.endsWith('/via/trechos/7/mapa') && r.params.get('faixaM') === '100').flush(mapa);
    await fixture.whenStable();

    const tela = fixture.nativeElement as HTMLElement;
    const faixas = [...tela.querySelectorAll<HTMLElement>('.via__faixa')];
    expect(faixas).toHaveLength(2);
    // a faixa com peça ruim é a aberta, pintada de "Ruim", com o selo de 1 peça
    const aberta = tela.querySelector<HTMLElement>('.via__faixa.ativa')!;
    expect(aberta.style.getPropertyValue('--cor')).toBe('var(--nota-ruim)');
    expect(aberta.querySelector('.via__qtd')?.textContent?.trim()).toBe('1');
    expect(aberta.getAttribute('aria-label')).toContain('1 Ruim');
    // as duas faixas ocupam a via inteira, cada uma a metade
    expect(faixas.map((f) => f.style.getPropertyValue('--ini'))).toEqual(['0', '0.5']);
    // a régua de 100 em 100 m e a lupa com a peça ruim aberta
    expect([...tela.querySelectorAll('.via__km')].map((k) => k.textContent?.trim())).toEqual(['km 213,0', 'km 213,1', 'km 213,2']);
    expect(tela.querySelectorAll('.lupa__peca')).toHaveLength(1);
    // a lupa de 100 m (6 px por metro) marca o km de 25 em 25 m
    expect([...tela.querySelectorAll('.lupa__km span')].map((k) => k.textContent?.trim())).toEqual(['213,100', '213,125', '213,150', '213,175', '213,200']);
    expect(tela.querySelector('.lupa__aberta')?.textContent).toContain('L09-213106-0002');
    expect(tela.textContent).not.toContain('L09-213000-0001');
    http.verify();
  });
});
