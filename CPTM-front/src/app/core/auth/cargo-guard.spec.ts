import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, CanActivateFn, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { entrarComo } from '../../testes/sessao-de-teste';
import { cargoGuard } from './cargo-guard';

describe('cargoGuard', () => {
  const executar: CanActivateFn = (...params) => TestBed.runInInjectionContext(() => cargoGuard(...params));
  const rota = (cargos?: string[]) => ({ data: cargos ? { cargos } : {} }) as unknown as ActivatedRouteSnapshot;
  const estado = {} as RouterStateSnapshot;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
  });

  it('sem login manda para o login', () => {
    expect((executar(rota(), estado) as UrlTree).toString()).toBe('/login');
  });

  it('logado entra na rota sem cargo exigido', () => {
    entrarComo('OperadorVia');
    expect(executar(rota(), estado)).toBe(true);
  });

  it('cargo errado volta ao painel', () => {
    entrarComo('OperadorVia');
    expect((executar(rota(['Supervisor']), estado) as UrlTree).toString()).toBe('/painel');
  });

  it('cargo certo entra', () => {
    entrarComo('Supervisor');
    expect(executar(rota(['Supervisor', 'Tecnico']), estado)).toBe(true);
  });
});
