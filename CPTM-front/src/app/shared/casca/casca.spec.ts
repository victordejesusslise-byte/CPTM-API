import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Sessao } from '../../core/auth/sessao';
import { entrarComo } from '../../testes/sessao-de-teste';
import { Casca } from './casca';

describe('Casca', () => {
  const abrir = async (cargo: Parameters<typeof entrarComo>[0]) => {
    localStorage.clear();
    entrarComo(cargo);
    await TestBed.configureTestingModule({ imports: [Casca], providers: [provideRouter([]), provideHttpClient()] }).compileComponents();
    const fixture = TestBed.createComponent(Casca);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  };
  const itens = (tela: HTMLElement) => [...tela.querySelectorAll('.menu__item')].map((a) => a.textContent?.trim());

  it('o supervisor vê a supervisão inteira', async () => {
    const tela = await abrir('Supervisor');
    expect(itens(tela)).toContain('Usuários');
    expect(itens(tela)).toContain('Cadastros');
    expect(tela.querySelector('.topo__cargo')?.textContent).toBe('Supervisor');
  });

  it('o operador da via vê só o campo', async () => {
    const tela = await abrir('OperadorVia');
    expect(itens(tela)).toContain('Alertas');
    expect(itens(tela)).not.toContain('Pedidos');
    expect(itens(tela)).not.toContain('Usuários');
    expect(tela.querySelector('.topo__cargo')?.textContent).toBe('Operador da via');
  });

  it('em Pedidos abre Cadastrar pedido e Acompanhar pedidos embaixo do item', async () => {
    localStorage.clear();
    entrarComo('Supervisor');
    await TestBed.configureTestingModule({ imports: [Casca], providers: [provideRouter([{ path: 'pedidos', children: [] }, { path: 'pedidos/novo', children: [] }]), provideHttpClient()] }).compileComponents();
    const fixture = TestBed.createComponent(Casca);
    await TestBed.inject(Router).navigateByUrl('/pedidos/novo');
    await fixture.whenStable();
    const subs = [...(fixture.nativeElement as HTMLElement).querySelectorAll('.menu__sub')].map((a) => a.textContent?.trim());
    expect(subs).toEqual(['Cadastrar pedido', 'Acompanhar pedidos']);
    expect((fixture.nativeElement as HTMLElement).querySelector('.menu__sub--ativo')?.textContent?.trim()).toBe('Cadastrar pedido');
  });

  it('sair encerra a sessão e volta ao login', async () => {
    const tela = await abrir('Tecnico');
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    tela.querySelector<HTMLButtonElement>('.topo__sair')!.click();
    expect(TestBed.inject(Sessao).logado()).toBe(false);
    expect(navegar).toHaveBeenCalledWith(['/login']);
  });
});
