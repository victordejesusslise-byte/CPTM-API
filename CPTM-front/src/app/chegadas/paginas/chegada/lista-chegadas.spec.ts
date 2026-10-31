import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { ListaChegadas } from './lista-chegadas';

describe('ListaChegadas (Cargas e boletins)', () => {
  it('se a API recusa, mostra o erro em vez de ficar carregando (Victor, 2026-10-31)', async () => {
    localStorage.clear();
    entrarComo('Especialista');
    await TestBed.configureTestingModule({ imports: [ListaChegadas], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(ListaChegadas);
    fixture.componentRef.setInput('modo', 'boletins');
    fixture.detectChanges();
    TestBed.inject(HttpTestingController).expectOne('http://localhost:8045/api/pedidos').flush(null, { status: 403, statusText: 'Forbidden' });
    await fixture.whenStable();
    fixture.detectChanges();
    const tela = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(tela).toContain('Seu cargo não pode fazer isso.');
    expect(tela).not.toContain('Carregando');
  });
});
