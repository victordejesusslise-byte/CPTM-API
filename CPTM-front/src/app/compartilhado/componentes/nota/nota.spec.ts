import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Nota, classeNota } from './nota';

@Component({
  imports: [Nota],
  template: `<app-nota [nota]="nota()" />`,
})
class Hospede {
  readonly nota = signal<string | null>('Crítico');
}

describe('Nota', () => {
  it('cada nota tem a sua cor e a peça sem avaliação fica quieta', () => {
    expect(classeNota('Bom')).toContain('nota-peca--bom');
    expect(classeNota('Regular')).toContain('nota-peca--regular');
    expect(classeNota('Ruim')).toContain('nota-peca--ruim');
    expect(classeNota('Crítico')).toContain('nota-peca--critico');
    expect(classeNota(null)).toContain('pilula--quieta');
  });

  it('mostra a nota e "sem nota" quando não há avaliação', async () => {
    const fixture = TestBed.createComponent(Hospede);
    await fixture.whenStable();
    const pilula = (fixture.nativeElement as HTMLElement).querySelector('span')!;
    expect(pilula.textContent).toBe('Crítico');
    expect(pilula.classList).toContain('nota-peca--critico');

    fixture.componentInstance.nota.set(null);
    await fixture.whenStable();
    expect(pilula.textContent).toBe('sem nota');
  });
});
