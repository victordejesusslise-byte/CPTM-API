import { Directive, input } from '@angular/core';

/**
 * O botão vira skeleton enquanto envia (Victor, 2026-10-18: "o botão também terá o skeleton"): o texto some e uma barra corre para a
 * direita, como no skeleton YBYRA. Uso: <button [disabled]="enviando()" [appCarregando]="enviando()">. O estilo está no styles.scss.
 */
@Directive({
  selector: '[appCarregando]',
  host: { '[class.botao-carregando]': 'appCarregando()', '[attr.aria-busy]': 'appCarregando() ? true : null' },
})
export class Carregando {
  readonly appCarregando = input(false);
}
