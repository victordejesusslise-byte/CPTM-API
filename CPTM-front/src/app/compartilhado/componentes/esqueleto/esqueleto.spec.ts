import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PreviaLaudo } from '../../../laudos/componentes/previa-laudo';
import { Esqueleto } from './esqueleto';

describe('Esqueleto', () => {
  it('desenha o logo girando e as formas do que vai chegar, com o botão', async () => {
    await TestBed.configureTestingModule({ imports: [Esqueleto] }).compileComponents();
    const fixture = TestBed.createComponent(Esqueleto);
    fixture.componentRef.setInput('tipo', 'cartoes');
    fixture.componentRef.setInput('quantidade', 2);
    fixture.detectChanges();
    const tela = fixture.nativeElement as HTMLElement;

    expect(tela.querySelector('[role=status]')?.getAttribute('aria-label')).toBe('Carregando…');
    expect(tela.querySelector('app-marca.giro')).not.toBeNull();
    expect(tela.querySelectorAll('.cartao-sk')).toHaveLength(2);
    expect(tela.querySelectorAll('.b.botao')).toHaveLength(2);
  });

  it('dentro da tabela (linhas) não mostra o logo', async () => {
    await TestBed.configureTestingModule({ imports: [Esqueleto] }).compileComponents();
    const fixture = TestBed.createComponent(Esqueleto);
    fixture.componentRef.setInput('tipo', 'linhas');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('app-marca')).toBeNull();
  });
});

describe('PreviaLaudo', () => {
  it('carrega com o skeleton da folha, abre e fecha pela seta e lembra que fechou', async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({ imports: [PreviaLaudo], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(PreviaLaudo);
    fixture.componentRef.setInput('tipo', 'troca');
    fixture.componentRef.setInput('id', 5);
    fixture.detectChanges();
    const tela = fixture.nativeElement as HTMLElement;
    expect(tela.querySelector('app-esqueleto')).not.toBeNull();

    TestBed.inject(HttpTestingController).expectOne('http://localhost:8045/api/laudos/troca/5/documento')
      .flush({ titulo: 'Laudo da troca', codigo: 'LD-0005-v1', subtitulo: 'DM-9', geradoEm: '2026-10-18T10:00:00', secoes: [] });
    await fixture.whenStable();
    const cabeca = tela.querySelector<HTMLButtonElement>('button.cabeca')!;
    expect(cabeca.textContent).toContain('LD-0005-v1');
    expect(cabeca.getAttribute('aria-expanded')).toBe('true');
    expect(tela.querySelector('.folha-previa')).not.toBeNull();

    cabeca.click();
    await fixture.whenStable();
    expect(cabeca.getAttribute('aria-expanded')).toBe('false');
    expect(tela.querySelector('.folha-previa')).toBeNull();
    expect(localStorage.getItem('ybyra.laudo-previa-aberta')).toBe('nao');
  });
});
