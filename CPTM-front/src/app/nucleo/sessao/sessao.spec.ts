import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Rede } from '../fila/rede';
import { entrarComo, jwtDeTeste, tokenDeTeste, usuarioDeTeste } from '../../compartilhado/testes/sessao-de-teste';
import { Sessao, usuarioDoToken } from './sessao';

describe('Sessao', () => {
  const criar = () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    return { sessao: TestBed.inject(Sessao), http: TestBed.inject(HttpTestingController) };
  };

  beforeEach(() => localStorage.clear());

  it('com o login vencido e sem rede, continua; com rede, pede a senha (app Android)', () => {
    const online = signal(false);
    entrarComo('OperadorVia', -5);
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Rede, useValue: { online } }] });
    const sessao = TestBed.inject(Sessao);
    expect(sessao.logado()).toBe(true);
    online.set(true);
    expect(sessao.logado()).toBe(false);
  });

  it('abrir sem rede não derruba a sessão (a conferência fica para quando a rede voltar)', () => {
    entrarComo('Tecnico');
    const { sessao, http } = criar();
    let confirmou: boolean | undefined;
    sessao.confirmar().subscribe((c) => (confirmou = c));
    http.expectOne('http://localhost:8045/api/auth/eu').error(new ProgressEvent('error'), { status: 0 });
    expect(confirmou).toBe(true);
    expect(sessao.logado()).toBe(true);
  });

  it('entra pelo e-mail e senha e guarda só o token', () => {
    const { sessao, http } = criar();
    sessao.entrar('ana@cptm.sp.gov.br', 'segredo').subscribe();
    const req = http.expectOne('http://localhost:8045/api/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'ana@cptm.sp.gov.br', senha: 'segredo' });
    const resposta = tokenDeTeste('Tecnico');
    req.flush(resposta);

    expect(sessao.logado()).toBe(true);
    expect(sessao.usuario()?.cargo).toBe('Tecnico');
    expect(sessao.temCargo('Tecnico', 'Supervisor')).toBe(true);
    expect(sessao.temCargo('Supervisor')).toBe(false);
    const guardado = JSON.parse(localStorage.getItem('via-permanente.sessao')!);
    expect(guardado).toEqual({ token: resposta.token, expiraEm: resposta.expiraEm });
  });

  it('volta logado depois de recarregar a página, com o cargo de dentro do token', () => {
    entrarComo('OperadorPatio');
    const { sessao } = criar();
    expect(sessao.logado()).toBe(true);
    expect(sessao.usuario()?.cargo).toBe('OperadorPatio');
  });

  it('o cargo editado no armazenamento do navegador não vale', () => {
    // Quem edita pelo DevTools o "usuario" guardado (como era antes de 2026-10-23) não muda o cargo: ele vem do token.
    const sessaoAntiga = { ...tokenDeTeste('Tecnico'), usuario: usuarioDeTeste('Supervisor') };
    localStorage.setItem('via-permanente.sessao', JSON.stringify(sessaoAntiga));
    const { sessao } = criar();
    expect(sessao.usuario()?.cargo).toBe('Tecnico');
    expect(sessao.temCargo('Supervisor')).toBe(false);
  });

  it('confere com a API ao abrir e fica com o que a API diz', () => {
    entrarComo('Tecnico');
    const { sessao, http } = criar();
    let ok: boolean | undefined;
    sessao.confirmar().subscribe((r) => (ok = r));
    http.expectOne('http://localhost:8045/api/auth/eu').flush({ ...usuarioDeTeste('Tecnico'), nome: 'Nome da API' });
    expect(ok).toBe(true);
    expect(sessao.usuario()?.nome).toBe('Nome da API');
    // Já conferido: não pergunta de novo.
    sessao.confirmar().subscribe();
    http.expectNone('http://localhost:8045/api/auth/eu');
  });

  it('token recusado pela API encerra a sessão', () => {
    entrarComo('Supervisor');
    const { sessao, http } = criar();
    let ok: boolean | undefined;
    sessao.confirmar().subscribe((r) => (ok = r));
    http.expectOne('http://localhost:8045/api/auth/eu').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(ok).toBe(false);
    expect(sessao.logado()).toBe(false);
    expect(localStorage.getItem('via-permanente.sessao')).toBeNull();
  });

  it('token vencido não vale', () => {
    entrarComo('Supervisor', -5);
    const { sessao } = criar();
    expect(sessao.logado()).toBe(false);
  });

  it('token que não é JWT não vale', () => {
    localStorage.setItem('via-permanente.sessao', JSON.stringify({ token: 'qualquer-coisa', expiraEm: new Date(Date.now() + 60_000).toISOString() }));
    const { sessao } = criar();
    expect(sessao.logado()).toBe(false);
    expect(usuarioDoToken('a.b.c')).toBeNull();
    expect(usuarioDoToken(jwtDeTeste(usuarioDeTeste('Especialista')))?.cargo).toBe('Especialista');
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
