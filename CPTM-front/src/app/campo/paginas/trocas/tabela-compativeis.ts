import { Component, input, model } from '@angular/core';
import { CartoesNoCelular } from '../../../compartilhado/componentes/tabela/cartoes';
import { CompativelDto } from '../../entidades/campo';

/**
 * As peças compatíveis na tabela do sistema (Victor, 2026-11-17): uma linha por peça; o toque na linha escolhe a peça.
 * No celular cada linha vira um cartão (CartoesNoCelular), com a sugerida como etiqueta no canto.
 * Usada em "Trocas para separar" (troca avulsa) e em "Separar lote".
 */
@Component({
  selector: 'app-tabela-compativeis',
  imports: [CartoesNoCelular],
  styles: `
    /* No celular o fundo da célula some (styles.scss); a peça escolhida se marca no cartão inteiro, com a faixa à esquerda. */
    @media (max-width: 600px) {
      tr.selecionada { background: var(--superficie-3); box-shadow: inset 3px 0 0 var(--marcador); border-color: var(--tinta-3); }
      tr.selecionada td:first-child { box-shadow: none; }
    }
  `,
  template: `
    <div class="tabela">
      <table class="simples">
        <thead><tr><th>Peça</th><th>Espécie</th><th>Pilha</th><th>Situação</th></tr></thead>
        <tbody>
          @for (c of lista(); track c.peca.id) {
            <!-- A linha inteira escolhe (clique ou Enter), como nas outras tabelas do sistema. -->
            <tr class="clicavel" tabindex="0" [class.selecionada]="escolhida()?.peca?.id === c.peca.id" [attr.aria-selected]="escolhida()?.peca?.id === c.peca.id"
              (click)="escolhida.set(c)" (keydown.enter)="escolhida.set(c)">
              <td class="mono">{{ c.peca.codigo }}</td>
              <td>{{ c.peca.especie }} · {{ c.peca.classe }}ª</td>
              <td>{{ c.peca.pilha ?? '—' }} · {{ c.idadeDias }} dias no armazém</td>
              <!-- Só a sugerida tem texto; a célula vazia some no cartão do celular. -->
              <td>@if (c.sugerida) {sugerida}</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class TabelaCompativeis {
  /** as peças que servem, na ordem da API (sai primeiro a mais antiga). */
  readonly lista = input.required<CompativelDto[]>();
  /** a peça escolhida; a tela de fora lê e muda (a sugerida pode já vir escolhida). */
  readonly escolhida = model<CompativelDto | null>(null);
}
