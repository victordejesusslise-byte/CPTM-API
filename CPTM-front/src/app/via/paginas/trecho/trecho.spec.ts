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

  it('abre o trecho com peça, desenha uma barra por faixa e abre a faixa com peça ruim', async () => {
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
    expect(tela.querySelectorAll('.mapa-trecho__faixa')).toHaveLength(2);
    expect(tela.querySelector('.seg--ruim')).toBeTruthy();
    expect(tela.textContent).toContain('L09-213106-0002');
    expect(tela.textContent).not.toContain('L09-213000-0001');
    http.verify();
  });
});
