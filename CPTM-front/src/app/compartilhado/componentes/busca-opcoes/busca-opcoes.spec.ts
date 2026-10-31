import { Component, signal, viewChild } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatSelect, MatSelectModule } from '@angular/material/select';
import { BuscaNasOpcoes, normalizarBusca } from './busca-opcoes';

@Component({
  imports: [MatSelectModule, BuscaNasOpcoes],
  template: `
    <mat-select [value]="escolhido()" (selectionChange)="escolhido.set($event.value)">
      <app-busca-opcoes />
      @for (o of opcoes(); track o) { <mat-option [value]="o">{{ o }}</mat-option> }
    </mat-select>
  `,
})
class Tela {
  readonly opcoes = signal(['PD-001 · Três Guri', 'PD-002 · Madeireira São João', 'PD-003 · Três Guri', 'PD-004 · Ipê Forte', 'PD-005 · Três Guri',
    'PD-006 · Ipê Forte', 'PD-007 · Jatobá Ltda', 'PD-008 · Três Guri']);
  readonly escolhido = signal<string | null>(null);
  readonly select = viewChild.required(MatSelect);
}

describe('BuscaNasOpcoes (Victor, 2026-10-31: pesquisa nos campos de opções)', () => {
  const abrir = async (quantas?: number) => {
    await TestBed.configureTestingModule({ imports: [Tela] }).compileComponents();
    const fixture = TestBed.createComponent(Tela);
    if (quantas) fixture.componentInstance.opcoes.update((l) => l.slice(0, quantas));
    fixture.detectChanges();
    fixture.componentInstance.select().open();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  };
  const campo = () => document.querySelector<HTMLInputElement>('.busca-opcoes input');
  const visiveis = () => [...document.querySelectorAll('mat-option')].filter((o) => !o.classList.contains('opcao-escondida')).map((o) => o.textContent!.trim());
  const digitar = (texto: string) => {
    campo()!.value = texto;
    campo()!.dispatchEvent(new Event('input'));
  };
  const tecla = (key: string) => campo()!.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));

  it('sem acento e sem maiúscula', () => {
    expect(normalizarBusca('  São JOÃO ')).toBe('sao joao');
  });

  it('com 8 opções ou mais, filtra pelo texto; a seta e o Enter escolhem entre as que ficaram', async () => {
    const fixture = await abrir();
    expect(campo()).toBeTruthy();
    digitar('sao joao');
    fixture.detectChanges();
    expect(visiveis()).toEqual(['PD-002 · Madeireira São João']);
    digitar('ipe');
    fixture.detectChanges();
    expect(visiveis()).toEqual(['PD-004 · Ipê Forte', 'PD-006 · Ipê Forte']);
    tecla('ArrowDown');
    tecla('Enter');
    fixture.detectChanges();
    expect(fixture.componentInstance.escolhido()).toBe('PD-006 · Ipê Forte');
  });

  it('nada encontrado avisa; com poucas opções, a pesquisa nem aparece', async () => {
    const fixture = await abrir();
    digitar('xyz');
    fixture.detectChanges();
    expect(visiveis()).toEqual([]);
    expect(document.querySelector('.busca-opcoes')?.textContent).toContain('Nada encontrado');
    fixture.componentInstance.select().close();
    fixture.detectChanges();
    TestBed.resetTestingModule();
    document.querySelectorAll('.cdk-overlay-container').forEach((c) => c.remove());
    await abrir(5);
    expect(campo()).toBeNull();
  });
});
