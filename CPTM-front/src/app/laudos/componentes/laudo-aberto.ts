import { Component, ElementRef, inject, input, signal, viewChild } from '@angular/core';
import { BOTOES } from '../../compartilhado/ui';
import { baixarArquivo } from '../../compartilhado/utilitarios/arquivo';
import { dataHora, mensagemDeErro } from '../../compartilhado/utilitarios/formato';
import { DocumentoLaudoDto, Laudos, TipoLaudo } from '../servicos/laudos';
import { FolhaLaudo } from './folha-laudo';

/**
 * O laudo aberto na tela, como no Figma e no protótipo: a janela com o título e o código, Ampliar, Baixar PDF e Fechar, e a folha
 * do laudo. Baixar só existe aqui: primeiro se vê o laudo, depois se baixa (Victor, 2026-10-18). O PDF é o mesmo documento.
 */
@Component({
  selector: 'app-laudo-aberto',
  imports: [BOTOES, FolhaLaudo],
  template: `
    <dialog #janela class="laudo-dlg" [class.zoom]="ampliado()" (close)="ampliado.set(false)" [attr.aria-label]="documento().titulo + ' ' + documento().codigo">
      <div class="topo">
        <div class="tit"><h2>{{ documento().titulo }} {{ documento().codigo }}</h2><span>gerado em {{ dataHora(documento().geradoEm) }}</span></div>
        @if (situacao()) { <span class="st" role="status">{{ situacao() }}</span> }
        <button mat-button type="button" (click)="ampliado.set(!ampliado())" [attr.aria-pressed]="ampliado()">{{ ampliado() ? 'Reduzir' : 'Ampliar' }}</button>
        <button mat-flat-button type="button" (click)="baixar()" [disabled]="baixando()">{{ baixando() ? 'Baixando…' : 'Baixar PDF' }}</button>
        <button mat-button type="button" (click)="fechar()">Fechar</button>
      </div>
      <div class="paginas" tabindex="0" aria-label="Páginas do laudo"><app-folha-laudo [documento]="documento()" /></div>
    </dialog>
  `,
  styles: `
    .laudo-dlg { border: 0; padding: 0; margin: auto; width: min(880px, calc(100vw - 32px)); height: calc(100dvh - 32px); max-width: none; max-height: none;
      border-radius: 10px; background: var(--superficie); color: var(--tinta); overflow: hidden; }
    .laudo-dlg[open] { display: flex; flex-direction: column; }
    .laudo-dlg::backdrop { background: rgba(13, 13, 13, .6); }
    .topo { display: flex; flex-wrap: wrap; align-items: center; gap: 6px 8px; padding: 10px 14px; border-bottom: 1px solid var(--linha); }
    .tit { flex: 1 1 220px; min-width: 0; }
    .tit h2 { margin: 0; font-size: 16px; }
    .tit span, .st { font-size: 12.5px; color: var(--tinta-2); }
    .st { flex-basis: 100%; order: 3; }
    .paginas { flex: 1; overflow: auto; padding: 18px; background: var(--superficie-3); }
    .zoom .paginas app-folha-laudo { width: 1100px; }
    .zoom .paginas ::ng-deep .folha { max-width: none; }
    @media (max-width: 620px) { .laudo-dlg { width: 100vw; height: 100dvh; border-radius: 0; } .paginas { padding: 10px; } .zoom .paginas app-folha-laudo { width: 900px; } }
  `,
})
export class LaudoAberto {
  readonly tipo = input.required<TipoLaudo>();
  readonly id = input.required<number>();
  readonly documento = input.required<DocumentoLaudoDto>();
  private readonly laudos = inject(Laudos);
  private readonly janela = viewChild.required<ElementRef<HTMLDialogElement>>('janela');
  protected readonly ampliado = signal(false);
  protected readonly baixando = signal(false);
  protected readonly situacao = signal<string | null>(null);
  protected readonly dataHora = dataHora;

  abrir(): void {
    this.situacao.set(null);
    this.janela().nativeElement.showModal();
  }

  protected fechar(): void {
    this.janela().nativeElement.close();
  }

  protected baixar(): void {
    this.baixando.set(true);
    this.situacao.set(null);
    this.laudos.pdf(this.tipo(), this.id()).subscribe({
      next: (pdf) => {
        baixarArquivo(pdf, `${this.documento().codigo}.pdf`);
        this.baixando.set(false);
        this.situacao.set(`Baixado: ${this.documento().codigo}.pdf`);
      },
      error: (e) => {
        this.baixando.set(false);
        this.situacao.set(mensagemDeErro(e));
      },
    });
  }
}
