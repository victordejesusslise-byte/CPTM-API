import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, CanActivateFn, GuardResult, MaybeAsync, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { Observable } from 'rxjs';
import { entrarComo, usuarioDeTeste } from '../../compartilhado/testes/sessao-de-teste';
import { Cargo } from '../../pessoas/entidades/usuario';
import { cargoGuard } from './cargo-guard';

describe('cargoGuard', () => {
  const executar: CanActivateFn = (...params) => TestBed.runInInjectionContext(() => cargoGuard(...params));
  const rota = (cargos?: string[]) => ({ data: cargos ? { cargos } : {} }) as unknown as ActivatedRouteSnapshot;
  const estado = {} as RouterStateSnapshot;
  const eu = 'http://localhost:8045/api/auth/eu';

  /** Roda a guarda e responde o /auth/eu com o cargo dado (ou com 401). */
  const resultado = (saida: MaybeAsync<GuardResult>, cargoDaApi: Cargo | 401): GuardResult | undefined => {
    let r: GuardResult | undefined;
    (saida as Observable<GuardResult>).subscribe((v) => (r = v));
    const req = TestBed.inject(HttpTestingController).expectOne(eu);
    if (cargoDaApi === 401) req.flush(null, { status: 401, statusText: 'Unauthorized' });
    else req.flush(usuarioDeTeste(cargoDaApi));
    return r;
  };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
  });

  it('sem login manda para o login', () => {
    expect((executar(rota(), estado) as UrlTree).toString()).toBe('/login');
  });

  it('logado entra na rota sem cargo exigido, depois de a API confirmar', () => {
    entrarComo('OperadorVia');
    expect(resultado(executar(rota(), estado), 'OperadorVia')).toBe(true);
  });

  it('cargo errado volta ao painel', () => {
    entrarComo('OperadorVia');
    expect((resultado(executar(rota(['Supervisor']), estado), 'OperadorVia') as UrlTree).toString()).toBe('/painel');
  });

  it('cargo certo entra', () => {
    entrarComo('Supervisor');
    expect(resultado(executar(rota(['Supervisor', 'Tecnico']), estado), 'Supervisor')).toBe(true);
  });

  it('vale o cargo que a API confirma, não o do aparelho', () => {
    entrarComo('Supervisor');
    expect((resultado(executar(rota(['Supervisor']), estado), 'Tecnico') as UrlTree).toString()).toBe('/painel');
  });

  it('token recusado pela API volta ao login', () => {
    entrarComo('Supervisor');
    expect((resultado(executar(rota(['Supervisor']), estado), 401) as UrlTree).toString()).toBe('/login');
  });
});
