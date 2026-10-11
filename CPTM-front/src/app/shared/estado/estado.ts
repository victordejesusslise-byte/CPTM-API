import { Component, input } from '@angular/core';
import { CARREGANDO } from '../ui';

/**
 * O estado de uma lista ou tela: carregando, erro (em vermelho, com a mensagem da API) ou vazio.
 * Uso: <app-estado [carregando]="x.isLoading()" [erro]="erro()" [vazio]="!x.value()?.length" textoVazio="Nenhum pedido." />
 */
@Component({
  selector: 'app-estado',
  imports: [CARREGANDO],
  template: `
    @if (carregando()) {
      <mat-progress-bar mode="indeterminate" aria-label="Carregando" />
    } @else if (erro()) {
      <p class="aviso aviso--erro" role="alert">{{ erro() }}</p>
    } @else if (vazio()) {
      <p class="aviso">{{ textoVazio() }}</p>
    }
  `,
})
export class Estado {
  readonly carregando = input(false);
  readonly erro = input<string | null>(null);
  readonly vazio = input(false);
  readonly textoVazio = input('Nada por aqui ainda.');
}