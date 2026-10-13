import { Component, computed, input, output } from '@angular/core';
import { MatTableModule } from '@angular/material/table';

export interface Coluna<T> {
  titulo: string;
  valor: (linha: T) => string | number | null | undefined;
  mono?: boolean;
  /** Vermelho só para exceção: divergência, reprovada, o que pede ação. */
  vermelho?: (linha: T) => boolean;
  /** Mostra o valor numa pílula com esta classe (ex.: a nota da peça). */
  pilula?: (linha: T) => string;
}

/**
 * A tabela do sistema (Angular Material por dentro). Uso:
 * <app-tabela [colunas]="colunas" [linhas]="pedidos" [clicavel]="true" (escolher)="abrir($event)" />
 */
@Component({
  selector: 'app-tabela',
  imports: [MatTableModule],
  template: `
    <div class="tabela">
      <table mat-table [dataSource]="linhas()">
        @for (c of colunas(); track c.titulo; let i = $index) {
          <ng-container [matColumnDef]="'c' + i">
            <th mat-header-cell *matHeaderCellDef>{{ c.titulo }}</th>
            <td mat-cell *matCellDef="let l" [class.mono]="c.mono" [class.vermelho]="c.vermelho?.(l)">@if (c.pilula) { <span [class]="c.pilula(l)">{{ c.valor(l) ?? '—' }}</span> } @else { {{ c.valor(l) ?? '—' }} }</td>
          </ng-container>
        }
        <tr mat-header-row *matHeaderRowDef="nomes()"></tr>
        <tr mat-row *matRowDef="let l; columns: nomes()" [class.clicavel]="clicavel()" [attr.tabindex]="clicavel() ? 0 : null"
          (click)="clicavel() && escolher.emit(l)" (keydown.enter)="clicavel() && escolher.emit(l)"></tr>
      </table>
    </div>
  `,
})
export class Tabela<T> {
  readonly colunas = input.required<Coluna<T>[]>();
  readonly linhas = input.required<readonly T[]>();
  readonly clicavel = input(false);
  readonly escolher = output<T>();
  protected readonly nomes = computed(() => this.colunas().map((_, i) => 'c' + i));
}