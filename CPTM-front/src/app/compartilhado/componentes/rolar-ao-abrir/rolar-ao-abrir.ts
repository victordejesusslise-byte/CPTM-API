import { Directive, ElementRef, afterRenderEffect, inject, input } from '@angular/core';

/** A tela do celular: a mesma largura em que a casca vira a versão de celular (casca.scss). */
const CELULAR = '(max-width: 820px)';
/** A barra do alto do celular (56 px) e uma folga: o detalhe para logo abaixo dela. */
const ALTURA_DA_BARRA = 64;

/**
 * No celular, quando um item da lista abre, a tela rola até o começo do detalhe (Victor, 2026-11-22).
 * Sem isso, o detalhe abre lá em cima ou lá embaixo, fora da vista, e parece que o toque não fez nada.
 * Uso: <section [rolarAoAbrir]="item.id">. Rola de novo quando a chave muda (outro item aberto). No computador, nada muda.
 */
@Directive({
  selector: '[rolarAoAbrir]',
  // Ao rolar, o detalhe para abaixo da barra do alto (que fica fixa), não atrás dela.
  host: { '[style.scroll-margin-top]': '"calc(' + ALTURA_DA_BARRA + 'px + var(--topo-seguro, 0px))"' },
})
export class RolarAoAbrir {
  /** A chave do item aberto (o id): quando ela muda, a tela rola de novo. */
  readonly rolarAoAbrir = input<unknown>();
  private readonly elemento = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

  constructor() {
    // Roda depois que o detalhe aparece na tela, e de novo a cada chave nova.
    afterRenderEffect(() => {
      this.rolarAoAbrir();
      // Só no celular: no computador a lista e o detalhe ficam como hoje.
      if (typeof matchMedia !== 'function' || !matchMedia(CELULAR).matches) return;
      // Se o começo do detalhe já está na parte de cima da tela, não mexe (a pessoa já está vendo).
      const topo = this.elemento.getBoundingClientRect().top;
      if (topo >= ALTURA_DA_BARRA && topo <= innerHeight * 0.6) return;
      this.elemento.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }
}
