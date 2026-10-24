import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Catalogos } from '../../catalogos/servicos/catalogos';
import { ChaveModulo } from './leitor';
import { Cenario, LeitorSimulado } from './leitor-simulado';

/**
 * O painel do módulo simulado (até o de verdade existir): conectar no USB-C, a chave A/N/V e as etiquetas por perto (só a peça,
 * com vizinhas longe, alvo em dúvida, nenhuma). As telas leem pelo botão "Ler com o módulo" do campo da tag.
 */
@Component({
  selector: 'app-painel-leitor',
  imports: [FormsModule],
  template: `
    <button type="button" class="modulo" (click)="alternar()" [attr.aria-expanded]="aberto()" aria-controls="painel-leitor">
      <span class="modulo__ponto" [class.modulo__ponto--ligado]="leitor.conectado()" aria-hidden="true"></span>
      Módulo · {{ leitor.chave() }}
    </button>
    @if (aberto()) {
      <section id="painel-leitor" class="painel" aria-label="Módulo RFID simulado">
        <header class="painel__topo">
          <strong>Módulo RFID</strong><span class="painel__rotulo">simulado</span>
          <button type="button" class="painel__fechar" (click)="aberto.set(false)" aria-label="Fechar o painel do módulo">×</button>
        </header>
        <label class="painel__linha">
          <input type="checkbox" [checked]="leitor.conectado()" (change)="conectar($any($event.target).checked)" /> Conectado no USB-C
        </label>
        <div class="painel__chave" role="radiogroup" aria-label="Chave do módulo">
          @for (c of chaves; track c) {
            <button type="button" role="radio" [attr.aria-checked]="leitor.chave() === c" (click)="leitor.trocarModo(c)">{{ c }} · {{ leitor.modos()[c].nome }}</button>
          }
        </div>
        <p class="painel__rotulo">{{ leitor.modo().potenciaDbm }} dBm · alvo único com {{ leitor.modo().folgaRssiDb }} dB de folga</p>
        <label class="painel__campo">Tag da peça à frente
          <input class="mono" [ngModel]="tag()" (ngModelChange)="tag.set($event)" autocomplete="off" spellcheck="false" />
        </label>
        <div class="painel__cenarios">
          @for (c of cenarios; track c.valor) {
            <button type="button" (click)="leitor.montar(c.valor, tag())">{{ c.rotulo }}</button>
          }
        </div>
        <ul class="painel__vistas">
          @for (t of leitor.porPerto(); track t.tag) {
            <li><span class="mono">{{ t.tag }}</span><span>{{ t.rssiDbm }} dBm</span></li>
          } @empty {
            <li class="painel__rotulo">Nenhuma etiqueta por perto.</li>
          }
        </ul>
      </section>
    }
  `,
  styles: `
    :host { position: fixed; right: 12px; bottom: calc(12px + var(--fundo-seguro)); z-index: 20; display: flex; flex-direction: column; align-items: flex-end; gap: 8px; }
    .modulo { order: 2; display: inline-flex; align-items: center; gap: 6px; min-height: 36px; padding: 0 12px; border: 1px solid var(--linha-2); border-radius: 18px;
      background: var(--superficie); color: var(--tinta); font: inherit; font-size: 13px; box-shadow: var(--sombra); cursor: pointer; opacity: .9; }
    .modulo__ponto { width: 8px; height: 8px; border-radius: 50%; background: var(--vinho); }
    .modulo__ponto--ligado { background: var(--verde); }
    .painel { order: 1; width: min(320px, calc(100vw - 24px)); max-height: 70vh; overflow: auto; padding: 12px; border: 1px solid var(--linha); border-radius: 8px;
      background: var(--superficie); color: var(--tinta); box-shadow: var(--sombra); display: flex; flex-direction: column; gap: 10px; font-size: 14px; }
    .painel__topo { display: flex; align-items: center; gap: 8px; }
    .painel__fechar { margin-left: auto; min-width: 36px; min-height: 36px; border: 0; background: transparent; color: inherit; font-size: 20px; cursor: pointer; }
    .painel__rotulo { margin: 0; font-size: 12px; color: var(--tinta-3); }
    .painel__linha { display: flex; align-items: center; gap: 8px; min-height: 36px; }
    .painel__chave { display: grid; grid-template-columns: repeat(3, 1fr); gap: 4px; }
    .painel__chave button, .painel__cenarios button { min-height: 36px; border: 1px solid var(--linha-2); border-radius: 6px; background: transparent; color: inherit; font: inherit; font-size: 13px; cursor: pointer; }
    .painel__chave button[aria-checked='true'] { background: var(--bloco); color: var(--sobre-escuro); border-color: var(--bloco); }
    .painel__campo { display: flex; flex-direction: column; gap: 4px; font-size: 12px; color: var(--tinta-3); }
    .painel__campo input { min-height: 36px; padding: 0 8px; border: 1px solid var(--linha-2); border-radius: 6px; background: var(--superficie-2); color: var(--tinta); font-size: 14px; }
    .painel__cenarios { display: grid; grid-template-columns: 1fr 1fr; gap: 4px; }
    .painel__vistas { margin: 0; padding: 0; list-style: none; display: flex; flex-direction: column; gap: 2px; }
    .painel__vistas li { display: flex; justify-content: space-between; gap: 8px; font-size: 13px; }
  `,
})
export class PainelLeitor {
  protected readonly leitor = inject(LeitorSimulado);
  private readonly catalogos = inject(Catalogos);
  protected readonly aberto = signal(false);
  protected readonly tag = signal('');
  protected readonly chaves: ChaveModulo[] = ['A', 'N', 'V'];
  protected readonly cenarios: { valor: Cenario; rotulo: string }[] = [
    { valor: 'uma', rotulo: 'Só a peça' },
    { valor: 'vizinhas', rotulo: 'Com vizinhas longe' },
    { valor: 'duvida', rotulo: 'Alvo em dúvida' },
    { valor: 'nenhuma', rotulo: 'Nenhuma' },
  ];
  private modosCarregados = false;

  protected alternar(): void {
    this.aberto.update((a) => !a);
    // Ao abrir, a potência e a folga que o supervisor deixou no servidor (sem rede, ficam as de começo).
    if (this.aberto() && !this.modosCarregados)
      this.catalogos.modosLeitura().subscribe({ next: (l) => ((this.modosCarregados = true), this.leitor.usarModosDoServidor(l)), error: () => undefined });
  }

  protected conectar(ligado: boolean): void {
    if (ligado) this.leitor.conectar();
    else this.leitor.desconectar();
  }
}
