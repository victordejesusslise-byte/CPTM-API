import { Component, computed, inject, input, linkedSignal, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MetodoCaptura } from '../../../compartilhado/entidades/enums';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { km, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { valorDe } from '../../../compartilhado/utilitarios/recurso';
import { FORMULARIO } from '../../../compartilhado/ui';
import { CompativelDto, LoteTrocaDto } from '../../entidades/campo';
import { LotesTroca } from '../../servicos/lotes-troca';
import { ROTULO_LOTE, aSepararNoLote } from './lote-aberto';

/** Separar peça, no lote de troca (Victor, 2026-11-11): o pátio separa até a quantidade aprovada, lendo a tag de cada peça; o sistema sugere a da frente. */
@Component({
  selector: 'app-separar-lote',
  imports: [FORMULARIO, LeituraTag],
  styles: `
    .barra { height: 8px; border-radius: 999px; background: var(--superficie-3); overflow: hidden; margin: 10px 0 4px; }
    .barra i { display: block; height: 100%; background: var(--areia); }
    p.nota, .lbl { margin: 8px 0; }
  `,
  template: `
    @let l = lote();
    <section class="cartao pilha">
      <header>
        <h2>Lote {{ l.codigo }}</h2><span class="pilula">{{ rotulo[l.status] }}</span>
        <span class="nota">@if (l.noiteDoPlano) { noite de {{ l.noiteDoPlano.slice(8, 10) }}/{{ l.noiteDoPlano.slice(5, 7) }} }<button mat-button type="button" (click)="fechar.emit()">Fechar</button></span>
      </header>
      @if (erro()) { <p class="aviso aviso--erro" role="alert">{{ erro() }}</p> }
      <div class="totais">
        <span class="t">Separar<b>{{ aprovadas() }}</b></span>
        <span class="t">Separadas<b>{{ l.separadas.length }}</b></span>
        <span class="t">Faltam<b [class.vermelho]="faltam() > 0">{{ faltam() }}</b></span>
        <span class="t">Tamanho<b>{{ metros(l.comprimentoM) }} · {{ l.classeExigida }}ª</b></span>
      </div>
      @if (obrigatoriasSemPeca().length || obrigatoriasComPeca().length) {
        <p class="nota">{{ l.quantidadeAprovada }} aprovadas + {{ obrigatoriasSemPeca().length }} das trocas obrigatórias@for (o of obrigatoriasComPeca(); track o.idTroca) { · {{ o.separada!.codigo }} já está separada no avulso de {{ o.peca.codigo }} }</p>
      }
      <div class="barra"><i [style.width.%]="aprovadas() ? (100 * l.separadas.length) / aprovadas() : 0"></i></div>
      <p class="nota">{{ l.trecho }} · <span class="mono">km {{ km(l.kmInicio) }} a {{ km(l.kmFim) }}</span></p>

      @if (faltam() > 0) {
        <p class="lbl">Peças que servem · {{ metros(l.comprimentoM) }}, classe que o trecho aceita · sai primeiro a mais antiga</p>
        @if (valorDe(compativeis)?.length) {
          <div class="opcoes">
            @for (c of (valorDe(compativeis) ?? []).slice(0, verOutras() ? 6 : 1); track c.peca.id) {
              <label class="opcao" [class.marcada]="escolhida()?.peca?.id === c.peca.id">
                <input type="radio" name="compativel-lote" [checked]="escolhida()?.peca?.id === c.peca.id" (change)="escolhida.set(c)" />
                <span class="mono">{{ c.peca.codigo }}</span>
                <span>{{ c.peca.especie }} · pilha {{ c.peca.pilha ?? '—' }}</span>
                <span class="nota">{{ c.sugerida ? 'sugerida · ' : '' }}{{ c.idadeDias }} dias no armazém</span>
              </label>
            }
          </div>
          @if (!verOutras() && (valorDe(compativeis)?.length ?? 0) > 1) {
            <button mat-button type="button" (click)="verOutras.set(true)">Ver outras {{ (valorDe(compativeis)?.length ?? 1) - 1 }} peças que servem</button>
          }
          @if (escolhida(); as c) {
            <app-leitura-tag [rotulo]="'Leia a tag da peça ' + c.peca.codigo" [compacta]="true" [(tag)]="tag" [(metodo)]="metodo" (lida)="separar()" />
            <div class="acoes"><button mat-flat-button type="button" (click)="separar()" [disabled]="enviando()" [appCarregando]="enviando()">Separar esta peça</button></div>
          }
        } @else if (compativeis.hasValue()) {
          <p class="aviso aviso--erro">Não há peça compatível no armazém. O lote fica esperando peça.</p>
        }
      } @else {
        <p class="aviso aviso--ok">Pronto: todas as peças aprovadas estão separadas. A via já pode trocar.</p>
      }

      @if (l.separadas.length) {
        <p class="lbl">Já separadas</p>
        <p class="mono">{{ codigos() }}</p>
      }
    </section>
  `,
})
export class SepararLote {
  protected readonly valorDe = valorDe;
  readonly lote = input.required<LoteTrocaDto>();
  readonly mudou = output<string>();
  readonly fechar = output<void>();
  private readonly api = inject(LotesTroca);
  protected readonly rotulo = ROTULO_LOTE;
  protected readonly km = km;
  protected readonly metros = metros;

  /** As trocas avulsas já aprovadas na faixa somam às peças a separar; a que já tem peça separada no avulso usa aquela (Victor, 2026-11-12). */
  protected readonly obrigatoriasSemPeca = computed(() => (this.lote().obrigatorias ?? []).filter((o) => o.situacao === 'Aberta' && !o.separada));
  protected readonly obrigatoriasComPeca = computed(() => (this.lote().obrigatorias ?? []).filter((o) => o.situacao === 'Aberta' && !!o.separada));
  protected readonly aprovadas = computed(() => aSepararNoLote(this.lote()));
  protected readonly faltam = computed(() => Math.max(0, this.aprovadas() - this.lote().separadas.length));
  protected readonly codigos = computed(() => this.lote().separadas.map((p) => p.codigo).join(', '));
  protected readonly compativeis = rxResource({
    params: () => (this.faltam() > 0 ? this.lote().id : undefined),
    stream: ({ params }) => this.api.compativeis(params),
  });

  readonly verOutras = signal(false);
  /** A sugerida (a do armazém mais perto, que entrou primeiro) já vem escolhida; o pátio só lê a tag. */
  readonly escolhida = linkedSignal<CompativelDto[] | undefined, CompativelDto | null>({
    source: () => valorDe(this.compativeis),
    computation: (lista) => lista?.find((c) => c.sugerida) ?? null,
  });
  readonly tag = signal('');
  readonly metodo = signal<MetodoCaptura>('ModuloUhf');
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  separar(): void {
    const c = this.escolhida();
    if (!c || !this.tag().trim()) {
      this.erro.set('Escolha a peça e leia a tag dela no armazém.');
      return;
    }
    if (c.peca.tag && c.peca.tag !== this.tag().trim()) {
      this.erro.set(`A tag lida não é a da peça ${c.peca.codigo}. Confira se pegou a peça certa.`);
      return;
    }
    this.enviando.set(true);
    this.erro.set(null);
    this.api.separar(this.lote().id, c.peca.id, leitura(this.tag(), this.metodo())).subscribe({
      next: () => {
        this.enviando.set(false);
        this.escolhida.set(null);
        this.tag.set('');
        this.compativeis.reload();
        this.mudou.emit(`Peça ${c.peca.codigo} separada para o lote ${this.lote().codigo}.`);
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }
}
