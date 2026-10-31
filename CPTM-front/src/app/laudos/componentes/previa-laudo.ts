import { Component, computed, inject, input, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { BOTOES } from '../../compartilhado/ui';
import { mensagemDeErro } from '../../compartilhado/utilitarios/formato';
import { Esqueleto } from '../../compartilhado/componentes/esqueleto/esqueleto';
import { Laudos, TipoLaudo } from '../servicos/laudos';
import { FolhaLaudo } from './folha-laudo';
import { LaudoAberto } from './laudo-aberto';
import { valorDe } from '../../compartilhado/utilitarios/recurso';

const CHAVE_ABERTO = 'ybyra.laudo-previa-aberta';

/** Começa aberta; se a pessoa fechou, continua fechada nas próximas (como os grupos do menu). Sem armazenamento, fica aberta. */
const lerAberto = (): boolean => {
  try { return localStorage.getItem(CHAVE_ABERTO) !== 'nao'; } catch { return true; }
};

/**
 * A primeira visão do laudo (Victor, 2026-10-18): a folha na própria tela, sem baixar. "Abrir o laudo" (ou um toque na folha) abre a
 * janela do laudo aberto, como no Figma, e só lá se baixa o PDF. A linha do alto abre e fecha a prévia, com a seta do menu.
 * Serve para qualquer laudo: BR, LI, LB e LD.
 */
@Component({
  selector: 'app-previa-laudo',
  imports: [BOTOES, FolhaLaudo, LaudoAberto, Esqueleto],
  template: `
    <button type="button" class="cabeca" [attr.aria-expanded]="aberta()" (click)="alternar()">
      <span>{{ valorDe(documento) ? valorDe(documento)!.titulo + ' · ' + valorDe(documento)!.codigo : 'Laudo' }}</span>
      <svg class="seta" [class.seta--aberta]="aberta()" viewBox="0 0 12 12" width="12" height="12" aria-hidden="true">
        <path d="M2.5 4.5 6 8l3.5-3.5" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </button>
    @if (aberta()) {
      @if (valorDe(documento); as d) {
        <div class="folha-previa" role="button" tabindex="0" aria-label="Abrir o laudo" (click)="aberto.abrir()" (keydown.enter)="aberto.abrir()">
          <app-folha-laudo [documento]="d" />
        </div>
        <div class="acoes">
          <button mat-flat-button type="button" (click)="aberto.abrir()">Abrir o laudo</button>
          @if (podeFechar()) { <button mat-button type="button" (click)="fechar.emit()">Fechar</button> }
        </div>
        <app-laudo-aberto #aberto [tipo]="tipo()" [id]="id()" [documento]="d" />
      } @else if (documento.isLoading()) {
        <app-esqueleto tipo="folha" [quantidade]="2" rotulo="Carregando o laudo…" />
      } @else if (erro()) {
        <p class="aviso aviso--erro">{{ erro() }}</p>
      }
    }
  `,
  styles: `
    :host { display: block; margin-top: 8px; }
    .cabeca { display: flex; width: 100%; align-items: center; justify-content: space-between; gap: 10px; min-height: 40px; padding: 6px 2px;
      border: 0; border-bottom: 1px solid var(--linha); background: transparent; color: var(--tinta); font: inherit; font-weight: 600; text-align: left; cursor: pointer; }
    .seta { flex-shrink: 0; transform: rotate(-90deg); transition: transform .2s; }
    .seta--aberta { transform: none; }
    @media (prefers-reduced-motion: reduce) { .seta { transition: none; } }
    .folha-previa { position: relative; margin-top: 10px; max-height: 340px; overflow: hidden; padding: 12px; border: 1px solid var(--linha); border-radius: 8px;
      background: var(--superficie-3); cursor: zoom-in; user-select: none; }
    .folha-previa::after { content: ''; position: absolute; inset: auto 0 0 0; height: 90px; background: linear-gradient(transparent, var(--superficie-3)); pointer-events: none; }
    .folha-previa:focus-visible { outline: 2px solid var(--marcador); outline-offset: 2px; }
  `,
})
export class PreviaLaudo {
  protected readonly valorDe = valorDe;
  readonly tipo = input.required<TipoLaudo>();
  readonly id = input.required<number>();
  /** Mostra o botão Fechar (nas listas, a prévia abre embaixo do item tocado). */
  readonly podeFechar = input(false);
  readonly fechar = output<void>();
  private readonly laudos = inject(Laudos);
  protected readonly aberta = signal(lerAberto());
  protected readonly documento = rxResource({ params: () => ({ tipo: this.tipo(), id: this.id() }), stream: ({ params }) => this.laudos.documento(params.tipo, params.id) });
  protected readonly erro = computed(() => (this.documento.error() ? mensagemDeErro(this.documento.error()) : null));

  protected alternar(): void {
    this.aberta.set(!this.aberta());
    try { localStorage.setItem(CHAVE_ABERTO, this.aberta() ? 'sim' : 'nao'); } catch { /* sem armazenamento: vale só nesta tela */ }
  }
}
