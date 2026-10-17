import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { entrarComo, tokenDeTeste } from '../../compartilhado/testes/sessao-de-teste';
import { Sessao } from './sessao';

describe('Sessao', () => {
  const criar = () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    return { sessao: TestBed.inject(Sessao), http: TestBed.inject(HttpTestingController) };
  };

  beforeEach(() => localStorage.clear());

  it('entra pelo e-mail e senha e guarda o token', () => {
    const { sessao, http } = criar();
    sessao.entrar('ana@cptm.sp.gov.br', 'segredo').subscribe();
    const req = http.expectOne('http://localhost:8045/api/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'ana@cptm.sp.gov.br', senha: 'segredo' });
    req.flush(tokenDeTeste('Tecnico'));

    expect(sessao.logado()).toBe(true);
    expect(sessao.usuario()?.cargo).toBe('Tecnico');
    expect(sessao.temCargo('Tecnico', 'Supervisor')).toBe(true);
    expect(sessao.temCargo('Supervisor')).toBe(false);
    expect(localStorage.getItem('via-permanente.sessao')).toContain('abc.def.ghi');
  });

  it('volta logado depois de recarregar a página', () => {
    entrarComo('OperadorPatio');
    const { sessao } = criar();
    expect(sessao.logado()).toBe(true);
    expect(sessao.token()).toBe('abc.def.ghi');
  });

  it('token vencido não vale', () => {
    entrarComo('Supervisor', -5);
    const { sessao } = criar();
    expect(sessao.logado()).toBe(false);
  });

  it('sair apaga a sessão', () => {
    entrarComo();
    const { sessao } = criar();
    sessao.sair();
    expect(sessao.logado()).toBe(false);
    expect(sessao.usuario()).toBeNull();
    expect(localStorage.getItem('via-permanente.sessao')).toBeNull();
  });
});
