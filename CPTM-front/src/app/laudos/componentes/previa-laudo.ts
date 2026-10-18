import { Component, computed, inject, input, output } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { BOTOES } from '../../compartilhado/ui';
import { mensagemDeErro } from '../../compartilhado/utilitarios/formato';
import { Laudos, TipoLaudo } from '../servicos/laudos';
import { FolhaLaudo } from './folha-laudo';
import { LaudoAberto } from './laudo-aberto';

/**
 * A primeira visão do laudo (Victor, 2026-10-18): a folha na própria tela, sem baixar. "Abrir o laudo" (ou um toque na folha) abre a
 * janela do laudo aberto, como no Figma, e só lá se baixa o PDF. Serve para qualquer laudo: BR, LI, LB e LD.
 */
@Component({
  selector: 'app-previa-laudo',
  imports: [BOTOES, FolhaLaudo, LaudoAberto],
  template: `
    @if (documento.value(); as d) {
      <div class="folha-previa" role="button" tabindex="0" aria-label="Abrir o laudo" (click)="aberto.abrir()" (keydown.enter)="aberto.abrir()">
        <app-folha-laudo [documento]="d" />
      </div>
      <div class="acoes">
        <button mat-flat-button type="button" (click)="aberto.abrir()">Abrir o laudo</button>
        @if (podeFechar()) { <button mat-button type="button" (click)="fechar.emit()">Fechar</button> }
      </div>
      <app-laudo-aberto #aberto [tipo]="tipo()" [id]="id()" [documento]="d" />
    } @else if (documento.isLoading()) {
      <p class="nota">Carregando o laudo…</p>
    } @else if (erro()) {
      <p class="aviso aviso--erro">{{ erro() }}</p>
    }
  `,
  styles: `
    :host { display: block; margin-top: 8px; }
    .folha-previa { position: relative; max-height: 340px; overflow: hidden; padding: 12px; border: 1px solid var(--linha); border-radius: 8px;
      background: var(--superficie-3); cursor: zoom-in; user-select: none; }
    .folha-previa::after { content: ''; position: absolute; inset: auto 0 0 0; height: 90px; background: linear-gradient(transparent, var(--superficie-3)); pointer-events: none; }
    .folha-previa:focus-visible { outline: 2px solid var(--marcador); outline-offset: 2px; }
  `,
})
export class PreviaLaudo {
  readonly tipo = input.required<TipoLaudo>();
  readonly id = input.required<number>();
  /** Mostra o botão Fechar (nas listas, a prévia abre embaixo do item tocado). */
  readonly podeFechar = input(false);
  readonly fechar = output<void>();
  private readonly laudos = inject(Laudos);
  protected readonly documento = rxResource({ params: () => ({ tipo: this.tipo(), id: this.id() }), stream: ({ params }) => this.laudos.documento(params.tipo, params.id) });
  protected readonly erro = computed(() => (this.documento.error() ? mensagemDeErro(this.documento.error()) : null));
}
