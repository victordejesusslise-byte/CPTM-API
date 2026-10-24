import { Component, computed, inject, signal } from '@angular/core';
import { dataHora } from '../../compartilhado/utilitarios/formato';
import { Fila } from './fila';

/**
 * A faixa da fila embaixo do alto da tela: "Sem rede · 3 esperando", "Enviando · 2" e "Não enviados · 1" (abre a lista, com o motivo,
 * para tentar de novo ou descartar). Some quando não há nada para mostrar.
 */
@Component({
  selector: 'app-estado-fila',
  template: `
    @if (!fila.online() || esperando() || fila.naoEnviados().length) {
      <div class="faixa" [class.faixa--sem-rede]="!fila.online()" role="status">
        @if (!fila.online()) {
          <span>Sem rede{{ esperando() ? ' · ' + esperando() + ' esperando' : '' }}</span>
        } @else if (esperando()) {
          <span>Enviando · {{ esperando() }}</span>
        }
        @if (fila.naoEnviados().length) {
          <button type="button" class="faixa__botao" (click)="aberta.set(!aberta())" [attr.aria-expanded]="aberta()">Não enviados · {{ fila.naoEnviados().length }}</button>
        }
      </div>
      @if (aberta() && fila.naoEnviados().length) {
        <ul class="lista" aria-label="Não enviados">
          @for (item of fila.naoEnviados(); track item.id) {
            <li>
              <strong>{{ item.rotulo }}</strong>
              <span class="lista__quando">{{ dataHora(item.criadoEm) }}</span>
              <p class="aviso aviso--erro">{{ item.motivo }}</p>
              <div class="lista__acoes">
                <button type="button" (click)="fila.tentarDeNovo(item)">Tentar de novo</button>
                <button type="button" (click)="fila.descartar(item)">Descartar</button>
              </div>
            </li>
          }
        </ul>
      }
    }
  `,
  styles: `
    .faixa { display: flex; flex-wrap: wrap; align-items: center; gap: 8px 16px; padding: 8px 16px; background: var(--superficie-3); color: var(--tinta); font-size: 14px; }
    .faixa--sem-rede { background: var(--madeira); color: var(--sobre-escuro); }
    .faixa__botao { margin-left: auto; min-height: 36px; padding: 0 12px; border: 1px solid currentColor; border-radius: 6px; background: transparent; color: inherit; font: inherit; cursor: pointer; }
    .lista { margin: 0; padding: 8px 16px; list-style: none; display: flex; flex-direction: column; gap: 12px; background: var(--superficie-2); border-bottom: 1px solid var(--linha); }
    .lista li { display: flex; flex-direction: column; gap: 4px; }
    .lista__quando { font-size: 12px; color: var(--tinta-3); }
    .lista .aviso { margin: 0; }
    .lista__acoes { display: flex; gap: 8px; }
    .lista__acoes button { min-height: 40px; padding: 0 12px; border: 1px solid var(--linha-2); border-radius: 6px; background: transparent; color: inherit; font: inherit; cursor: pointer; }
  `,
})
export class EstadoFila {
  protected readonly fila = inject(Fila);
  protected readonly esperando = computed(() => this.fila.meus().length);
  protected readonly aberta = signal(false);
  protected readonly dataHora = dataHora;
}
