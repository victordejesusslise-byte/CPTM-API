import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LeitorSimulado } from '../../../nucleo/leitor/leitor-simulado';
import { MetodoCaptura } from '../../entidades/enums';
import { LeituraTag, leitura } from './leitura-tag';

@Component({
  imports: [LeituraTag],
  template: '<app-leitura-tag [(tag)]="tag" [(metodo)]="metodo" (lida)="lidas = lidas + 1" />',
})
class Tela {
  readonly tag = signal('');
  readonly metodo = signal<MetodoCaptura>('ModuloUhf');
  lidas = 0;
}

describe('LeituraTag com o módulo', () => {
  const abrir = async () => {
    const fixture = TestBed.createComponent(Tela);
    await fixture.whenStable();
    const botao = () => (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.leitura__modulo button')!;
    const ler = async () => {
      botao().click();
      await vi.waitFor(() => expect(botao().disabled).toBe(false));
      await fixture.whenStable();
    };
    return { fixture, ler, tela: fixture.nativeElement as HTMLElement };
  };

  it('"Ler com o módulo" preenche a tag do alvo e a leitura leva o modo e o sinal', async () => {
    TestBed.inject(LeitorSimulado).montar('vizinhas', 'BR0000000002');
    const { fixture, ler } = await abrir();
    await ler();
    expect(fixture.componentInstance.tag()).toBe('BR0000000002');
    expect(fixture.componentInstance.lidas).toBe(1);
    expect(leitura('BR0000000002', 'ModuloUhf')).toMatchObject({ tag: 'BR0000000002', idModo: 2, rssiDbm: -45, tagsVistas: 1 });
  });

  it('com o alvo em dúvida, não preenche e diz por quê', async () => {
    TestBed.inject(LeitorSimulado).montar('duvida', 'BR0000000002');
    const { fixture, ler, tela } = await abrir();
    await ler();
    expect(fixture.componentInstance.tag()).toBe('');
    expect(tela.textContent).toContain('Mais de um dormente próximo');
  });
});
