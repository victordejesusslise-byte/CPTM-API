import { DestroyRef, Directive, ElementRef, afterNextRender, inject } from '@angular/core';

/** As colunas que viram a etiqueta no canto do cartão. */
const DESTAQUE = /^(situação|status|resultado|estado|nota)$/i;

/**
 * No celular, as tabelas viram cartões (Victor, 2026-10-24: opção A): cada linha é um cartão com a 1ª coluna no alto, a situação como
 * etiqueta no canto e as outras colunas embaixo, cada uma com o nome dela. Esta diretiva só põe o nome da coluna em cada célula
 * (data-titulo) e marca a coluna de destaque; quem desenha o cartão é o CSS de styles.scss (até 600 px). No computador, nada muda.
 * Vale para toda <table class="simples"> e para a tabela comum (mat-table) do componente que a importa.
 */
@Directive({ selector: 'table.simples, table[mat-table]', host: { class: 'cartoes' } })
export class CartoesNoCelular {
  private readonly tabela = inject<ElementRef<HTMLTableElement>>(ElementRef).nativeElement;

  constructor() {
    const destruir = inject(DestroyRef);
    afterNextRender(() => {
      this.rotular();
      // As linhas chegam e mudam depois (a lista carrega, filtra): rotula de novo quando o conteúdo muda.
      const observador = new MutationObserver(() => this.rotular());
      observador.observe(this.tabela, { childList: true, subtree: true });
      destruir.onDestroy(() => observador.disconnect());
    });
  }

  private rotular(): void {
    const titulos = [...this.tabela.querySelectorAll('thead tr:first-child th')].map((th) => th.textContent?.trim() ?? '');
    for (const linha of this.tabela.querySelectorAll('tbody tr')) {
      let coluna = 0;
      for (const celula of linha.children) {
        if (!(celula instanceof HTMLTableCellElement) || celula.tagName !== 'TD') continue;
        const titulo = titulos[coluna] ?? '';
        if (titulo) celula.setAttribute('data-titulo', titulo);
        else celula.removeAttribute('data-titulo');
        celula.classList.toggle('destaque', coluna > 0 && DESTAQUE.test(titulo));
        coluna += celula.colSpan || 1;
      }
    }
  }
}
