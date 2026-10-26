import { BreakpointObserver } from '@angular/cdk/layout';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { NO_APP } from '../../ao-vivo/trabalho-da-noite';
import { Rede } from '../../fila/rede';
import { Casca } from './casca';

describe('Casca no celular (app)', () => {
  const online = signal(true);
  const abrir = async () => {
    localStorage.clear();
    entrarComo('OperadorVia');
    await TestBed.configureTestingModule({
      imports: [Casca],
      providers: [
        provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
        { provide: NO_APP, useValue: true }, { provide: Rede, useValue: { online } },
        { provide: BreakpointObserver, useValue: { observe: () => of({ matches: true, breakpoints: {} }) } },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(Casca);
    await fixture.whenStable();
    await new Promise((pronto) => setTimeout(pronto, 350)); // o skeleton curto de toda tela
    fixture.detectChanges();
    return { fixture, tela: fixture.nativeElement as HTMLElement };
  };
  const tocar = (alvo: HTMLElement, tipo: 'touchstart' | 'touchend', x: number, y = 300) => {
    const toque = { clientX: x, clientY: y };
    alvo.dispatchEvent(Object.assign(new Event(tipo, { bubbles: true }), tipo === 'touchstart' ? { touches: [toque] } : { touches: [], changedTouches: [toque] }));
  };

  beforeEach(() => online.set(true));

  it('sem rede, numa tela que precisa de rede, avisa e mostra as telas que funcionam', async () => {
    online.set(false);
    const { tela } = await abrir();
    expect(tela.textContent).toContain('Esta tela precisa de rede');
    expect([...tela.querySelectorAll('.sem-rede__telas a')].map((a) => a.textContent?.trim())).toEqual(['Trocas do trecho', 'Consultar peça', 'Alertas (vistoria)']);
  });

  it('com rede, não avisa', async () => {
    const { tela } = await abrir();
    expect(tela.textContent).not.toContain('Esta tela precisa de rede');
  });

  it('deslizar o dedo para a direita abre o menu; para a esquerda, fecha', async () => {
    const { fixture, tela } = await abrir();
    const conteudo = tela.querySelector<HTMLElement>('main')!;
    const menu = () => tela.querySelector('#menu-lateral')!.classList.contains('menu--aberto');
    tocar(conteudo, 'touchstart', 40);
    tocar(conteudo, 'touchend', 200);
    fixture.detectChanges();
    expect(menu()).toBe(true);
    tocar(conteudo, 'touchstart', 300);
    tocar(conteudo, 'touchend', 100);
    fixture.detectChanges();
    expect(menu()).toBe(false);
    // Um toque curto ou para baixo não mexe no menu.
    tocar(conteudo, 'touchstart', 40, 100);
    tocar(conteudo, 'touchend', 80, 400);
    fixture.detectChanges();
    expect(menu()).toBe(false);
  });
});
