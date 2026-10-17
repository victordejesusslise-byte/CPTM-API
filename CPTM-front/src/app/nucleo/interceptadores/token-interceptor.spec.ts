import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { entrarComo } from '../../compartilhado/testes/sessao-de-teste';
import { Sessao } from '../sessao/sessao';
import { tokenInterceptor } from './token-interceptor';

describe('tokenInterceptor', () => {
  const criar = () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(withInterceptors([tokenInterceptor])), provideHttpClientTesting()],
    });
    return { http: TestBed.inject(HttpClient), teste: TestBed.inject(HttpTestingController) };
  };

  beforeEach(() => localStorage.clear());

  it('põe o token no cabeçalho', () => {
    entrarComo();
    const { http, teste } = criar();
    http.get('/api/pedidos').subscribe();
    expect(teste.expectOne('/api/pedidos').request.headers.get('Authorization')).toBe('Bearer abc.def.ghi');
  });

  it('sem sessão, vai sem cabeçalho', () => {
    const { http, teste } = criar();
    http.get('/api/pedidos').subscribe();
    expect(teste.expectOne('/api/pedidos').request.headers.has('Authorization')).toBe(false);
  });

  it('401 encerra a sessão e volta ao login', () => {
    entrarComo();
    const { http, teste } = criar();
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    http.get('/api/pedidos').subscribe({ error: () => undefined });
    teste.expectOne('/api/pedidos').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(TestBed.inject(Sessao).logado()).toBe(false);
    expect(navegar).toHaveBeenCalledWith(['/login']);
  });
});
