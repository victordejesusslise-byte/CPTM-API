import { Component, input } from '@angular/core';

/** O logo YBYRA: os anéis da tora com o corte em cruz (PDF da marca). A cor vem de quem usa (currentColor). */
@Component({
  selector: 'app-marca',
  template: `
    <svg [attr.width]="tamanho()" [attr.height]="tamanho()" viewBox="0 0 64 64" aria-hidden="true">
      <g fill="none" stroke="currentColor" stroke-linecap="round">
        <circle cx="32" cy="32" r="28" stroke-width="5" />
        @for (r of aneis; track r) {
          <circle cx="32" cy="32" [attr.r]="r" stroke-width="1.4" />
        }
        <path d="M14 14 L50 50 M50 14 L14 50" stroke-width="1.6" />
        <path d="M32 4 L32 60" stroke-width="1.2" />
      </g>
    </svg>
    @if (comNome()) {
      <span class="marca">YBYRA</span>
    }
  `,
  styles: `
    :host { display: inline-flex; align-items: center; gap: 10px; }
    .marca { font-size: 1.05em; }
  `,
})
export class Marca {
  readonly tamanho = input(28);
  readonly comNome = input(true);
  protected readonly aneis = [6, 10, 14, 18, 22];
}