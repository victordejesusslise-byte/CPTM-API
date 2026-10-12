import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { entrarComo } from '../../testes/sessao-de-teste';
import { SepararPeca } from './separar-peca';

describe('SepararPeca', () => {
  it('busca as trocas aprovadas e as que esperam peça', async () => {
    localStorage.clear();
    entrarComo('OperadorPatio');
    await TestBed.configureTestingModule({ imports: [SepararPeca], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(SepararPeca);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const pedidos = http.match((r) => r.url === 'http://localhost:5169/api/trocas');
    expect(pedidos.map((r) => r.request.params.get('status')).sort()).toEqual(['AguardandoPeca', 'Aprovada']);
    pedidos.forEach((r) => r.flush([]));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nenhuma troca aqui.');
  });
});
