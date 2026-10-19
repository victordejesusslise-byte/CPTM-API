import { Component, computed, input } from '@angular/core';
import { Marca } from '../marca/marca';

export type TipoEsqueleto = 'lista' | 'cartoes' | 'tabela' | 'folha' | 'pagina' | 'linhas' | 'formulario';

/**
 * O skeleton do sistema (Victor, 2026-10-18, estilo "D1 para a direita"): o logo YBYRA girando no alto e as formas do que vai chegar
 * (textos e botões) entrando em cascata pela esquerda e saindo pela direita. Quem pediu menos movimento no aparelho vê tudo parado.
 * Tipos: lista (itens), cartoes (indicadores, feixes), tabela, folha (laudo), pagina (tela inteira), formulario (campos e botão, nas telas de
 * cadastro) e linhas (dentro de uma tabela, sem o logo).
 */
@Component({
  selector: 'app-esqueleto',
  imports: [Marca],
  template: `
    <div class="esqueleto" role="status" aria-live="polite" [attr.aria-label]="rotulo()">
      <span class="sr">{{ rotulo() }}</span>
      @if (comMarca() && tipo() !== 'linhas') { <app-marca class="giro" [tamanho]="tipo() === 'pagina' ? 44 : 36" [comNome]="false" /> }
      @switch (tipo()) {
        @case ('cartoes') {
          <div class="cartoes">
            @for (n of itens(); track n) {
              <div class="cartao-sk">
                <i class="b" [style.width.%]="70" [style.animation-delay]="atraso(n, 0)"></i>
                <i class="b alta" [style.width.%]="45" [style.animation-delay]="atraso(n, 1)"></i>
                <i class="b" [style.width.%]="90" [style.animation-delay]="atraso(n, 2)"></i>
                <i class="b botao" [style.animation-delay]="atraso(n, 3)"></i>
              </div>
            }
          </div>
        }
        @case ('tabela') {
          <div class="tabela-sk">
            <div class="linha cabeca">@for (c of colunas; track $index) { <i class="b fina" [style.animation-delay]="atraso(0, $index)"></i> }</div>
            @for (n of itens(); track n) {
              <div class="linha">@for (c of colunas; track $index) { <i class="b" [style.width.%]="c" [style.animation-delay]="atraso(n, $index)"></i> }</div>
            }
          </div>
        }
        @case ('folha') {
          <div class="folha-sk">
            <i class="b fina" [style.width.%]="40" [style.animation-delay]="atraso(0, 0)"></i>
            <i class="b alta" [style.width.%]="60" [style.animation-delay]="atraso(0, 1)"></i>
            <i class="b" [style.width.%]="75" [style.animation-delay]="atraso(0, 2)"></i>
            @for (n of itens(); track n) {
              <i class="b fina titulo" [style.width.%]="30" [style.animation-delay]="atraso(n, 0)"></i>
              <i class="b" [style.width.%]="90" [style.animation-delay]="atraso(n, 1)"></i>
              <i class="b" [style.width.%]="80" [style.animation-delay]="atraso(n, 2)"></i>
            }
          </div>
          <i class="b botao" [style.animation-delay]="atraso(itens().length + 1, 0)"></i>
        }
        @case ('pagina') {
          <div class="pagina-sk">
            <i class="b fina" [style.width.%]="25" [style.animation-delay]="atraso(0, 0)"></i>
            <i class="b alta" [style.width.%]="50" [style.animation-delay]="atraso(0, 1)"></i>
            <div class="cartao-sk largo">
              @for (n of itens(); track n) { <i class="b" [style.width.%]="larguras[n % larguras.length]" [style.animation-delay]="atraso(n, 2)"></i> }
              <i class="b botao" [style.animation-delay]="atraso(itens().length, 3)"></i>
            </div>
          </div>
        }
        @case ('formulario') {
          <div class="formulario-sk">
            <div class="campos-sk">
              @for (n of itens(); track n) {
                <div class="campo-sk" [style.animation-delay]="atraso(n, 0)"><i class="b fina" [style.width.%]="40" [style.animation-delay]="atraso(n, 0)"></i></div>
              }
            </div>
            <i class="b botao" [style.animation-delay]="atraso(itens().length, 0)"></i>
          </div>
        }
        @case ('linhas') {
          @for (n of itens(); track n) { <i class="b" [style.width.%]="larguras[n % larguras.length]" [style.animation-delay]="atraso(n, 0)"></i> }
        }
        @default {
          <div class="lista-sk">
            @for (n of itens(); track n) {
              <div class="item-sk">
                <i class="b" [style.width.%]="55" [style.animation-delay]="atraso(n, 0)"></i>
                <i class="b" [style.width.%]="80" [style.animation-delay]="atraso(n, 1)"></i>
                <i class="b fina" [style.width.%]="35" [style.animation-delay]="atraso(n, 2)"></i>
              </div>
            }
          </div>
        }
      }
    </div>
  `,
  styles: `
    :host { display: block; }
    .esqueleto { display: grid; gap: 10px; padding: 6px 0; overflow: hidden; }
    .sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
    .giro { justify-self: center; color: var(--marcador); animation: girar 1.4s linear infinite; }
    .b { display: block; height: 12px; border-radius: 6px; background: color-mix(in srgb, var(--tinta) 16%, transparent); animation: paraDireita 1.8s ease-in-out infinite both; }
    .b.alta { height: 24px; }
    .b.fina { height: 9px; }
    .b.titulo { margin-top: 6px; }
    .b.botao { height: 36px; width: 46%; max-width: 200px; border-radius: 8px; }
    .cartoes { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 12px; }
    .cartao-sk { display: grid; gap: 9px; padding: 14px; border: 1px solid var(--linha); border-radius: 10px; background: var(--superficie); }
    .cartao-sk.largo { margin-top: 6px; }
    .lista-sk { display: grid; }
    .item-sk { display: grid; gap: 7px; padding: 11px 0; border-bottom: 1px solid var(--linha); }
    .item-sk:last-child { border-bottom: 0; }
    .tabela-sk { display: grid; gap: 0; }
    .linha { display: grid; grid-template-columns: 1.3fr 1fr .6fr 1fr; gap: 12px; padding: 10px 0; border-bottom: 1px solid var(--linha); }
    .linha.cabeca { padding-top: 2px; }
    .folha-sk { display: grid; gap: 8px; padding: 18px; border-radius: 8px; background: var(--superficie-2); }
    .pagina-sk { display: grid; gap: 10px; }
    .formulario-sk { display: grid; gap: 14px; padding: 16px; border: 1px solid var(--linha); border-radius: 10px; background: var(--superficie); }
    .campos-sk { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 12px; }
    .campo-sk { display: flex; align-items: center; height: 54px; padding: 0 14px; border: 1px solid var(--linha-2); border-radius: 6px;
      animation: paraDireita 1.8s ease-in-out infinite both; }
    @keyframes girar { to { transform: rotate(360deg); } }
    @keyframes paraDireita {
      0% { transform: translateX(-60%); opacity: 0; }
      25%, 75% { transform: translateX(0); opacity: 1; }
      100% { transform: translateX(60%); opacity: 0; }
    }
    @media (prefers-reduced-motion: reduce) { .giro, .b, .campo-sk { animation: none; opacity: 1; transform: none; } }
  `,
})
export class Esqueleto {
  readonly tipo = input<TipoEsqueleto>('lista');
  /** Quantos itens, cartões, linhas ou seções desenhar. */
  readonly quantidade = input(3);
  readonly rotulo = input('Carregando…');
  /** O logo girando no alto; desligue quando já há outro skeleton com o logo logo acima (um logo por tela). */
  readonly comMarca = input(true);
  protected readonly itens = computed(() => Array.from({ length: Math.max(1, this.quantidade()) }, (_, i) => i));
  protected readonly colunas = [85, 70, 50, 75];
  protected readonly larguras = [80, 65, 90, 55, 72];

  /** A cascata: cada forma começa um pouco depois da anterior. */
  protected atraso(item: number, parte: number): string {
    return `${((item * 4 + parte) * 0.08).toFixed(2)}s`;
  }
}
