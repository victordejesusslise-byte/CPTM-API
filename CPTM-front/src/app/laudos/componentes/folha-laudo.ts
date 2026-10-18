import { Component, input } from '@angular/core';
import { dataHora } from '../../compartilhado/utilitarios/formato';
import { DocumentoLaudoDto } from '../servicos/laudos';

/**
 * A folha do laudo desenhada na tela, igual ao PDF: título, "código · assunto", as seções em caixa alta com rótulo e valor, as tabelas
 * e o rodapé "gerado a partir das tabelas". É papel: fica branca também no modo escuro, como o PDF.
 */
@Component({
  selector: 'app-folha-laudo',
  template: `
    @let d = documento();
    <article class="folha" [attr.aria-label]="d.titulo + ' ' + d.codigo">
      <p class="marca">YBYRA · CPTM Via Permanente</p>
      <h2>{{ d.titulo }}</h2>
      <p class="codigo">{{ d.codigo }} · {{ d.subtitulo }}</p>
      @for (s of d.secoes; track $index) {
        <section>
          <h3>{{ s.titulo }}</h3>
          @if (s.campos.length) {
            <dl>
              @for (c of s.campos; track $index) { <div><dt>{{ c.rotulo }}</dt><dd>{{ c.valor }}</dd></div> }
            </dl>
          }
          @if (s.tabela; as t) {
            @if (t.linhas.length) {
              <div class="tabela"><table>
                <thead><tr>@for (c of t.colunas; track $index) { <th>{{ c }}</th> }</tr></thead>
                <tbody>@for (l of t.linhas; track $index) { <tr>@for (v of l; track $index) { <td>{{ v }}</td> }</tr> }</tbody>
              </table></div>
            } @else {
              <p class="vazio">Nenhum registro.</p>
            }
          }
        </section>
      }
      <p class="rodape">{{ d.codigo }} · gerado em {{ dataHora(d.geradoEm) }} a partir das tabelas</p>
    </article>
  `,
  styles: `
    :host { display: block; }
    .folha { box-sizing: border-box; width: 100%; max-width: 794px; margin: 0 auto; padding: 40px 44px 28px; background: #fff; color: #1a1a1a;
      font-family: Arial, Helvetica, sans-serif; font-size: 12px; line-height: 1.4; box-shadow: 0 1px 3px rgba(0, 0, 0, .18); border-radius: 2px; }
    .marca { margin: 0 0 14px; font-size: 10px; letter-spacing: .08em; text-transform: uppercase; color: #8a8a8a; }
    h2 { margin: 0; font-size: 21px; font-weight: 700; }
    .codigo { margin: 2px 0 14px; color: #666; }
    section { margin-top: 12px; }
    h3 { margin: 0 0 4px; padding-bottom: 2px; border-bottom: 1px solid #333; font-size: 10.5px; font-weight: 700; text-transform: uppercase; letter-spacing: .02em; }
    dl { margin: 0; }
    dl div { display: grid; grid-template-columns: minmax(110px, 190px) 1fr; gap: 8px; padding: 1px 0; }
    dt { color: #666; }
    dd { margin: 0; overflow-wrap: anywhere; }
    .tabela { overflow-x: auto; margin-top: 4px; }
    table { width: 100%; border-collapse: collapse; font-size: 11px; }
    th, td { border: 1px solid #d3d3d3; padding: 3px 5px; text-align: left; vertical-align: top; }
    th { background: #f5f5f5; font-weight: 700; }
    .vazio { margin: 2px 0; font-style: italic; color: #666; }
    .rodape { margin: 22px 0 0; font-size: 9px; color: #8a8a8a; }
    @media (max-width: 560px) { .folha { padding: 22px 16px 18px; font-size: 11px; } h2 { font-size: 17px; } dl div { grid-template-columns: 1fr; gap: 0; } }
  `,
})
export class FolhaLaudo {
  readonly documento = input.required<DocumentoLaudoDto>();
  protected readonly dataHora = dataHora;
}
