import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Coluna, Tabela } from './tabela';

interface Linha { codigo: string; situacao: string; }

@Component({
  imports: [Tabela],
  template: `<app-tabela [colunas]="colunas" [linhas]="linhas()" [clicavel]="true" (escolher)="escolhida = $event" />`,
})
class Hospede {
  readonly colunas: Coluna<Linha>[] = [
    { titulo: 'Código', valor: (l) => l.codigo, mono: true },
    { titulo: 'Situação', valor: (l) => l.situacao, vermelho: (l) => l.situacao === 'Diverge' },
  ];
  readonly linhas = signal<Linha[]>([{ codigo: 'P-1', situacao: 'Confere' }, { codigo: 'P-2', situacao: 'Diverge' }]);
  escolhida?: Linha;
}

describe('Tabela', () => {
  it('mostra as linhas, marca a exceção e avisa a escolhida', async () => {
    const fixture = TestBed.createComponent(Hospede);
    await fixture.whenStable();
    const tela = fixture.nativeElement as HTMLElement;
    expect(tela.querySelectorAll('tr.mat-mdc-row')).toHaveLength(2);
    expect(tela.querySelectorAll('td.vermelho')).toHaveLength(1);
    tela.querySelectorAll<HTMLElement>('tr.mat-mdc-row')[1].click();
    expect(fixture.componentInstance.escolhida?.codigo).toBe('P-2');
  });
});