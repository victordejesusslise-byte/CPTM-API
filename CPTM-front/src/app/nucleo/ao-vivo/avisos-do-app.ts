import { Component, inject } from '@angular/core';
import { TrabalhoDaNoite } from './trabalho-da-noite';

/** Os avisos do app embaixo do alto: troca urgente que chegou ao vivo e versão nova do app (com o link para baixar). */
@Component({
  selector: 'app-avisos-do-app',
  template: `
    @if (trabalho.urgente(); as aviso) {
      <div class="aviso-app aviso-app--urgente" role="alert">
        <span>{{ aviso }}</span>
        <button type="button" (click)="trabalho.urgente.set(null)">Ok</button>
      </div>
    }
    @if (trabalho.versaoNova(); as nova) {
      <div class="aviso-app" role="status">
        <span>Tem versão nova do app ({{ nova.versao }}).</span>
        @if (nova.baixar) { <a [href]="nova.baixar" target="_blank" rel="noopener">Baixar</a> }
        <button type="button" (click)="trabalho.versaoNova.set(null)">Depois</button>
      </div>
    }
  `,
  styles: `
    .aviso-app { display: flex; flex-wrap: wrap; align-items: center; gap: 8px 12px; padding: 8px 16px; background: var(--superficie-3); color: var(--tinta); font-size: 14px; }
    .aviso-app--urgente { background: var(--vinho); color: var(--sobre-escuro); }
    .aviso-app a { color: inherit; font-weight: 600; }
    .aviso-app button { margin-left: auto; min-height: 36px; padding: 0 12px; border: 1px solid currentColor; border-radius: 6px; background: transparent; color: inherit; font: inherit; cursor: pointer; }
  `,
})
export class AvisosDoApp {
  protected readonly trabalho = inject(TrabalhoDaNoite);
}
