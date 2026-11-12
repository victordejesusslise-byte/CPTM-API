import { Component, inject, output, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Catalogos } from '../../../catalogos/servicos/catalogos';
import { km, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { FORMULARIO } from '../../../compartilhado/ui';
import { Trechos } from '../../../via/servicos/trechos';
import { LoteTrocaDto } from '../../entidades/campo';
import { LotesTroca } from '../../servicos/lotes-troca';

/**
 * Pedir lote de troca (Victor, 2026-11-11): o técnico informa o trecho, a faixa de km, quantas peças e o comprimento, sem escolher as peças —
 * na noite, a via lê a tag de cada dormente que sai. O lote vai para Aprovar trocas.
 */
@Component({
  selector: 'app-pedir-lote',
  imports: [FormsModule, FORMULARIO],
  styles: `
    .largo { grid-column: 1 / -1; }
    .chamada { margin-bottom: 12px; }
  `,
  template: `
    <section class="cartao pilha">
      <header><h2>Pedir lote de troca</h2><span class="nota">vai para Aprovar trocas</span></header>
      <div class="chamada chamada--neutra"><b>As peças não são escolhidas agora.</b> Na noite da troca, a via lê a tag de cada dormente que sai e de cada um que entra.</div>
      @if (erro()) { <p class="aviso aviso--erro" role="alert">{{ erro() }}</p> }
      <div class="campos">
        <mat-form-field appearance="outline" class="largo"><mat-label>Trecho</mat-label>
          <mat-select [value]="idTrecho()" (selectionChange)="escolherTrecho($event.value)"><app-busca-opcoes />
            @for (t of trechos(); track t.id) { <mat-option [value]="t.id">{{ t.nome }} · km {{ km(t.kmInicio) }} a {{ km(t.kmFim) }}</mat-option> }
          </mat-select></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Km de</mat-label><input matInput type="number" step="0.001" class="mono" [ngModel]="kmInicio()" (ngModelChange)="kmInicio.set($event)" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Km até</mat-label><input matInput type="number" step="0.001" class="mono" [ngModel]="kmFim()" (ngModelChange)="kmFim.set($event)" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Quantidade de peças</mat-label><input matInput type="number" min="1" step="1" class="mono" [ngModel]="quantidade()" (ngModelChange)="quantidade.set($event)" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Comprimento</mat-label>
          <mat-select [value]="idTamanho()" (selectionChange)="idTamanho.set($event.value)"><app-busca-opcoes />
            @for (t of tamanhos(); track t.id) { <mat-option [value]="t.id">{{ metros(t.comprimentoM) }}</mat-option> }
          </mat-select></mat-form-field>
        <mat-form-field appearance="outline" class="largo"><mat-label>Motivo</mat-label><input matInput [ngModel]="motivo()" (ngModelChange)="motivo.set($event)" placeholder="Ex.: dormentes podres entre os km 23,1 e 23,4" /></mat-form-field>
        <mat-form-field appearance="outline" class="largo"><mat-label>Observação (opcional)</mat-label><input matInput [ngModel]="observacao()" (ngModelChange)="observacao.set($event)" placeholder="Ex.: aproveitar a interdição de quinta" /></mat-form-field>
      </div>
      @if (obrigatorias.hasValue() && obrigatorias.value().length) {
        <div class="chamada chamada--neutra"><b>Nesta faixa já há {{ obrigatorias.value().length }} troca{{ obrigatorias.value().length > 1 ? 's' : '' }} aprovada{{ obrigatorias.value().length > 1 ? 's' : '' }}</b>:
          @for (o of obrigatorias.value(); track o.idTroca) { <span class="mono">km {{ km(o.peca.kmProjeto) }}</span> · {{ o.peca.codigo }}{{ $last ? '.' : ';' }} }
          Elas entram no lote como obrigatórias, além das peças que você pedir.</div>
      }
      <div class="acoes">
        <button mat-flat-button type="button" (click)="pedir()" [disabled]="enviando()" [appCarregando]="enviando()">Pedir o lote</button>
        <button mat-stroked-button type="button" (click)="cancelar.emit()">Cancelar</button>
      </div>
    </section>
  `,
})
export class PedirLote {
  readonly pedido = output<LoteTrocaDto>();
  readonly cancelar = output<void>();
  private readonly api = inject(LotesTroca);
  protected readonly trechos = toSignal(inject(Trechos).listar(), { initialValue: [] });
  protected readonly tamanhos = toSignal(inject(Catalogos).tamanhos(), { initialValue: [] });
  protected readonly km = km;
  protected readonly metros = metros;

  readonly idTrecho = signal<number | null>(null);
  readonly kmInicio = signal<number | null>(null);
  readonly kmFim = signal<number | null>(null);
  readonly quantidade = signal<number | null>(null);
  readonly idTamanho = signal<number | null>(null);
  readonly motivo = signal('');
  /** As trocas avulsas já aprovadas na faixa escolhida: entram no lote como obrigatórias (Victor, 2026-11-12). */
  protected readonly obrigatorias = rxResource({
    params: () => {
      const [idTrecho, inicio, fim, idTamanho] = [this.idTrecho(), this.kmInicio(), this.kmFim(), this.idTamanho()];
      return idTrecho && idTamanho && inicio !== null && fim !== null && inicio <= fim ? { idTrecho, inicio, fim, idTamanho } : undefined;
    },
    stream: ({ params }) => this.api.obrigatorias(params.idTrecho, params.inicio, params.fim, params.idTamanho),
  });
  readonly observacao = signal('');
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  /** Escolher o trecho já põe a faixa do trecho inteiro; o técnico estreita. */
  escolherTrecho(id: number): void {
    this.idTrecho.set(id);
    const t = this.trechos().find((x) => x.id === id);
    if (t) {
      this.kmInicio.set(t.kmInicio);
      this.kmFim.set(t.kmFim);
    }
  }

  protected pedir(): void {
    const [idTrecho, inicio, fim, quantidade, idTamanho, motivo] = [this.idTrecho(), this.kmInicio(), this.kmFim(), this.quantidade(), this.idTamanho(), this.motivo().trim()];
    const falta = !idTrecho ? 'Escolha o trecho.'
      : inicio === null || fim === null ? 'Informe a faixa de km (do km… ao km…).'
      : inicio > fim ? 'O km de início vem antes do km do fim.'
      : !quantidade || quantidade < 1 ? 'Informe quantas peças.'
      : !idTamanho ? 'Escolha o comprimento.'
      : !motivo ? 'Escreva o motivo do lote.'
      : null;
    if (falta) {
      this.erro.set(falta);
      return;
    }
    this.enviando.set(true);
    this.erro.set(null);
    this.api.pedir({ idTrecho: idTrecho!, kmInicio: inicio!, kmFim: fim!, quantidade: quantidade!, idTamanho: idTamanho!, motivo, observacao: this.observacao().trim() || null }).subscribe({
      next: (lote) => {
        this.enviando.set(false);
        this.pedido.emit(lote);
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }
}
