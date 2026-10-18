import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { entrarComo } from '../../compartilhado/testes/sessao-de-teste';
import { Sessao } from '../sessao/sessao';
import { ambiente } from '../../../environments/ambiente';
import { ehDaApi, tokenInterceptor } from './token-interceptor';

const API = `${ambiente.api}/pedidos`;

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
    http.get(API).subscribe();
    expect(teste.expectOne(API).request.headers.get('Authorization')).toBe('Bearer abc.def.ghi');
  });

  it('sem sessão, vai sem cabeçalho', () => {
    const { http, teste } = criar();
    http.get(API).subscribe();
    expect(teste.expectOne(API).request.headers.has('Authorization')).toBe(false);
  });

  it('401 encerra a sessão e volta ao login', () => {
    entrarComo();
    const { http, teste } = criar();
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    http.get(API).subscribe({ error: () => undefined });
    teste.expectOne(API).flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(TestBed.inject(Sessao).logado()).toBe(false);
    expect(navegar).toHaveBeenCalledWith(['/login']);
  });

  it('nunca manda o token para endereço de fora (segurança, 2026-10-18)', () => {
    entrarComo();
    const { http, teste } = criar();
    http.get('https://auditoria.invalid/recurso').subscribe();
    http.get('http://localhost:9999/api/pedidos').subscribe();
    expect(teste.expectOne('https://auditoria.invalid/recurso').request.headers.has('Authorization')).toBe(false);
    expect(teste.expectOne('http://localhost:9999/api/pedidos').request.headers.has('Authorization')).toBe(false);
  });

  it('reconhece só a API do sistema', () => {
    expect(ehDaApi(API)).toBe(true);
    expect(ehDaApi('https://exemplo.com/api')).toBe(false);
    expect(ehDaApi(ambiente.api + 'x/pedidos')).toBe(false);
  });
});
