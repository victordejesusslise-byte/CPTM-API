import { Component, input } from '@angular/core';
import { Esqueleto, TipoEsqueleto } from '../esqueleto/esqueleto';

/**
 * O estado de uma lista ou tela: carregando (o skeleton YBYRA), erro (em vermelho, com a mensagem da API) ou vazio.
 * Uso: <app-estado [carregando]="x.isLoading()" [erro]="erro()" [vazio]="!x.value()?.length" textoVazio="Nenhum pedido." esqueleto="tabela" />
 */
@Component({
  selector: 'app-estado',
  imports: [Esqueleto],
  template: `
    @if (carregando()) {
      <app-esqueleto [tipo]="esqueleto()" [quantidade]="quantidade()" [comMarca]="comMarca()" />
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
  /** A forma do que vai chegar: lista (padrão), cartoes, tabela, folha ou pagina. */
  readonly esqueleto = input<TipoEsqueleto>('lista');
  readonly quantidade = input(3);
  /** O logo girando; desligue quando já há outro skeleton com o logo na tela. */
  readonly comMarca = input(true);
}
