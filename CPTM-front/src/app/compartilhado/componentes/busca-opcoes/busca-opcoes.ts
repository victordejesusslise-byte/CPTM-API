import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatSelect } from '@angular/material/select';

/** Com quantas opções a pesquisa aparece. */
const MINIMO_DE_OPCOES = 8;
/** A opção que não bate com a pesquisa (o CSS está em styles.scss, porque a lista abre fora da tela, no overlay). */
const ESCONDIDA = 'opcao-escondida';

/** Para comparar: sem acento, sem maiúscula e sem espaço nas pontas. */
export const normalizarBusca = (texto: string): string => texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();

/**
 * A pesquisa no alto da lista de um select (Victor, 2026-10-31: "pesquisa em campos de opções", como o filtro do p-select).
 * Vai dentro do <mat-select>, antes das opções: <app-busca-opcoes />. Só aparece com 8 opções ou mais; filtra pelo texto da opção,
 * sem acento e sem maiúscula. As setas e o Enter escolhem entre as que ficaram; Esc fecha. No computador, o cursor já abre na pesquisa.
 */
@Component({
  selector: 'app-busca-opcoes',
  template: `
    @if (mostrar()) {
      <div class="busca-opcoes">
        <input #campo type="search" placeholder="Pesquisar…" aria-label="Pesquisar nas opções" autocomplete="off"
          [value]="texto()" (input)="filtrar($any($event.target).value)" (keydown)="tecla($event)" />
        @if (nenhuma()) { <p>Nada encontrado.</p> }
      </div>
    }
  `,
  styles: `
    .busca-opcoes { position: sticky; top: 0; z-index: 1; padding: 8px 12px; background: var(--superficie); border-bottom: 1px solid var(--linha); }
    input { width: 100%; box-sizing: border-box; min-height: 40px; padding: 0 12px; border: 1px solid var(--linha-2); border-radius: 6px;
      background: var(--superficie-2); color: var(--tinta); font: inherit; }
    input:focus { outline: 2px solid var(--marcador); outline-offset: 1px; }
    p { margin: 10px 2px 2px; color: var(--tinta-3); font-size: 14px; }
  `,
})
export class BuscaNasOpcoes {
  private readonly select = inject(MatSelect);
  private readonly campo = viewChild<ElementRef<HTMLInputElement>>('campo');
  protected readonly mostrar = signal(false);
  protected readonly texto = signal('');
  protected readonly nenhuma = signal(false);

  constructor() {
    this.select.openedChange.pipe(takeUntilDestroyed()).subscribe((aberto) => {
      if (!aberto) return this.filtrar('');
      this.mostrar.set(this.select.options.length >= MINIMO_DE_OPCOES);
      // as setas pulam as opções escondidas; com a lista fechada, as desligadas, como o próprio select faz
      this.select._keyManager?.skipPredicate((o) => o._getHostElement().classList.contains(ESCONDIDA) || (!this.select.panelOpen && o.disabled));
      // no celular, o teclado só abre quando a pessoa toca na pesquisa (senão cobre a lista)
      if (this.mostrar() && typeof matchMedia === 'function' && matchMedia('(pointer: fine)').matches) setTimeout(() => this.campo()?.nativeElement.focus());
    });
  }

  protected filtrar(texto: string): void {
    this.texto.set(texto);
    const busca = normalizarBusca(texto);
    let visiveis = 0;
    for (const opcao of this.select.options) {
      const bate = !busca || normalizarBusca(opcao.viewValue).includes(busca);
      opcao._getHostElement().classList.toggle(ESCONDIDA, !bate);
      if (bate) visiveis++;
    }
    this.nenhuma.set(!!busca && visiveis === 0);
    const ativa = this.select._keyManager?.activeItem;
    if (busca && visiveis && (!ativa || ativa._getHostElement().classList.contains(ESCONDIDA))) this.select._keyManager?.setFirstItemActive();
  }

  /** As teclas da pesquisa: setas e Enter escolhem, Esc fecha; o resto é texto (não deixa o select pular para a opção pela letra). */
  protected tecla(evento: KeyboardEvent): void {
    evento.stopPropagation();
    const gerente = this.select._keyManager;
    if (evento.key === 'ArrowDown' || evento.key === 'ArrowUp') {
      evento.preventDefault();
      const descer = evento.key === 'ArrowDown';
      if (!gerente?.activeItem) {
        if (descer) gerente?.setFirstItemActive();
        else gerente?.setLastItemActive();
      } else if (descer) gerente.setNextItemActive();
      else gerente.setPreviousItemActive();
    } else if (evento.key === 'Enter') {
      evento.preventDefault();
      const ativa = gerente?.activeItem;
      if (ativa && !ativa._getHostElement().classList.contains(ESCONDIDA)) ativa._selectViaInteraction();
    } else if (evento.key === 'Escape') {
      evento.preventDefault();
      this.select.close();
      this.select.focus();
    }
  }
}
