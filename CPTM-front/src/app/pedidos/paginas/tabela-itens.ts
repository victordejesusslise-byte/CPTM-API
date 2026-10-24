import { Component, computed, input } from '@angular/core';
import { m3, metros } from '../../compartilhado/utilitarios/formato';

export interface ItemParaTabela { comprimentoM: number; classe: number; especie: string | null; medida: string | null; quantidade: number; volumeM3: number; }

/** Os itens do pedido com o total e a parte de 2ª classe (em m³), como na revisão do protótipo. */
@Component({
  selector: 'app-tabela-itens',
  template: `
    <div class="tabela">
      <table class="simples">
        <thead><tr><th>Comprimento</th><th>Classe</th><th>Espécie</th><th>Padrão</th><th class="num">Peças</th><th class="num">m³</th></tr></thead>
        <tbody>
          @for (i of itens(); track $index) {
            <tr><td>{{ metros(i.comprimentoM) }}</td><td>@if (i.classe === 2) { <b>2ª</b> } @else { 1ª }</td><td>{{ i.especie ?? 'qualquer da classe' }}</td><td class="mono">{{ i.medida ?? '—' }}</td>
              <td class="num">{{ i.quantidade }}</td><td class="num">{{ m3(i.volumeM3) }}</td></tr>
          }
          <tr class="total"><td colspan="4">Total · 2ª classe {{ segunda() }}% do m³</td><td class="num">{{ pecas() }}</td><td class="num">{{ m3(volume()) }}</td></tr>
        </tbody>
      </table>
    </div>
  `,
})
export class TabelaItens {
  readonly itens = input.required<ItemParaTabela[]>();
  protected readonly metros = metros;
  protected readonly m3 = m3;
  protected readonly pecas = computed(() => this.itens().reduce((a, i) => a + i.quantidade, 0));
  protected readonly volume = computed(() => this.itens().reduce((a, i) => a + i.volumeM3, 0));
  protected readonly segunda = computed(() => {
    const v = this.volume();
    const s = this.itens().filter((i) => i.classe === 2).reduce((a, i) => a + i.volumeM3, 0);
    return v ? (s / v * 100).toLocaleString('pt-BR', { maximumFractionDigits: 1 }) : '0';
  });
}