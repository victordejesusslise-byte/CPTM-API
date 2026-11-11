import { Component, computed, inject, input, linkedSignal, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { Sessao } from '../../../nucleo/sessao/sessao';
import { dataHora, km, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { FORMULARIO } from '../../../compartilhado/ui';
import { PreviaLaudo } from '../../../laudos/componentes/previa-laudo';
import { TipoLaudo } from '../../../laudos/servicos/laudos';
import { LoteTrocaDto, StatusLoteTroca } from '../../entidades/campo';
import { LotesTroca } from '../../servicos/lotes-troca';

export const ROTULO_LOTE: Record<StatusLoteTroca, string> = {
  Pedido: 'Pedido', Aprovado: 'Aprovado', AguardandoPeca: 'Aguardando peça', Separado: 'Separado', Feito: 'Feito',
  Reprovado: 'Reprovado', Recusado: 'Recusado pela via', Cancelado: 'Cancelado',
};

/**
 * O lote de troca aberto em Aprovar trocas e em Trocas e laudos (Victor, 2026-11-11): quem aprova decide o lote inteiro (pode baixar a quantidade,
 * com o porquê); o técnico responde à recusa da via; todos veem as trocas feitas no lote e os laudos do lote (LD e, depois de aprovado, LB).
 */
@Component({
  selector: 'app-lote-aberto',
  imports: [FormsModule, FORMULARIO, PreviaLaudo],
  styles: `
    .laudo-previa { margin-top: 8px; padding-top: 10px; border-top: 1px solid var(--linha); }
    .laudo-previa header { display: flex; flex-wrap: wrap; align-items: center; gap: 4px 10px; margin-bottom: 8px; }
    .laudo-previa h3 { margin: 0; font-size: 15px; }
    .laudo-previa .ativo { font-weight: 700; text-decoration: underline; text-underline-offset: 4px; }
    .totais { margin-bottom: 12px; }
    .chamada { margin: 10px 0; }
  `,
  template: `
    @let l = lote();
    <section class="cartao pilha">
      <header>
        <h2>Lote {{ l.codigo }}</h2><span class="pilula" [class.pilula--solida]="l.status === 'Feito'" [class.pilula--alerta]="l.status === 'Recusado' || l.status === 'Reprovado'">{{ rotulo[l.status] }}</span>
        <span class="nota"><button mat-button type="button" (click)="fechar.emit()">Fechar</button></span>
      </header>
      @if (erro()) { <p class="aviso aviso--erro" role="alert">{{ erro() }}</p> }
      <div class="totais">
        <span class="t">Pediu<b>{{ l.quantidadePedida }}</b></span>
        @if (l.quantidadeAprovada !== null) { <span class="t">Aprovadas<b>{{ l.quantidadeAprovada }}</b></span> }
        <span class="t">Comprimento<b>{{ metros(l.comprimentoM) }}</b></span>
        <span class="t">Classe do trecho<b>{{ l.classeExigida }}ª</b></span>
        @if (l.separadas.length || l.trocas.length) { <span class="t">Separadas<b>{{ l.separadas.length + l.trocas.length }}</b></span> }
        @if (l.trocas.length) { <span class="t">Trocadas<b>{{ l.trocas.length }}</b></span> }
      </div>
      <p class="lbl">Pedido</p>
      <p>{{ l.trecho }}, <span class="mono">km {{ km(l.kmInicio) }} a {{ km(l.kmFim) }}</span>. Motivo: {{ l.motivo }}@if (l.observacao) { · {{ l.observacao }} }</p>
      <p class="nota">Pedido em {{ dataHora(l.dataPedido) }}@if (l.dataAprovacao) { · decidido em {{ dataHora(l.dataAprovacao) }} }@if (l.motivoMenos) { · aprovou menos: {{ l.motivoMenos }} }</p>
      @if (l.avulsasAbertas.length) {
        <div class="chamada chamada--neutra">Neste trecho já tem <b>{{ l.avulsasAbertas.length }} pedido{{ l.avulsasAbertas.length > 1 ? 's' : '' }} avulso{{ l.avulsasAbertas.length > 1 ? 's' : '' }} aberto{{ l.avulsasAbertas.length > 1 ? 's' : '' }}</b>
          ({{ codigosAvulsas() }}). Se a via trocar essa peça no lote, o avulso fecha como feito no lote.</div>
      }
      @if (l.motivoReprovacao) { <p class="nota">Reprovado: {{ l.motivoReprovacao }}</p> }
      @if (l.cancelamento) { <p class="nota">Cancelado: {{ l.cancelamento }}</p> }

      @if (l.status === 'Pedido' && podeAprovar()) {
        <p class="lbl">Decisão</p>
        <div class="campos">
          <mat-form-field appearance="outline"><mat-label>Quantidade aprovada</mat-label><input matInput type="number" min="1" [max]="l.quantidadePedida" class="mono" [ngModel]="quantidade()" (ngModelChange)="quantidade.set($event)" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>{{ menos() ? 'Por que menos (obrigatório)' : 'Motivo (obrigatório para reprovar)' }}</mat-label><input matInput [ngModel]="motivo()" (ngModelChange)="motivo.set($event)" /></mat-form-field>
        </div>
        <mat-checkbox [checked]="urgente()" (change)="urgente.set($event.checked)">Urgente: entra no plano desta noite mesmo depois das 20:00</mat-checkbox>
        <div class="acoes">
          <button mat-flat-button type="button" (click)="decidir(true)" [disabled]="enviando()" [appCarregando]="enviando()">Aprovar o lote · {{ quantidade() }} peças</button>
          <button mat-stroked-button class="perigo" type="button" (click)="decidir(false)" [disabled]="enviando()" [appCarregando]="enviando()">Reprovar</button>
        </div>
      }

      @if (l.ultimaRecusa; as r) {
        <div class="chamada"><b>A via recusou em {{ dataHora(r.data) }}:</b> {{ r.motivo }}@if (r.descricao) { — {{ r.descricao }} } · foto <span class="mono">{{ r.fotoArquivo }}</span>
          @if (r.decisao) { <br />Técnico: {{ r.decisao === 'Manter' ? 'manteve o lote' : 'aceitou a recusa' }}@if (r.resposta) { — {{ r.resposta }} } }</div>
        @if (l.status === 'Recusado' && !r.decisao && ehTecnico()) {
          <mat-form-field appearance="outline"><mat-label>Resposta à via (obrigatória para manter)</mat-label><input matInput [ngModel]="resposta()" (ngModelChange)="resposta.set($event)" /></mat-form-field>
          <div class="acoes">
            <button mat-flat-button type="button" (click)="decidirRecusa('Manter')" [disabled]="enviando()" [appCarregando]="enviando()">Manter o lote</button>
            <button mat-stroked-button type="button" (click)="decidirRecusa('Aceitar')" [disabled]="enviando()" [appCarregando]="enviando()">Aceitar a recusa</button>
          </div>
        }
      }

      @if (l.trocas.length) {
        <p class="lbl">Trocas do lote · conferidas pela tag</p>
        <div class="cartoes-alerta">
          @for (t of l.trocas; track t.id) {
            <div class="cartao-alerta"><span class="topo-alerta"><b class="mono">{{ t.codigoSaiu }} → {{ t.codigoEntrou }}</b><span class="mono nota">km {{ km(t.km) }}</span></span>
              <span class="nota">{{ dataHora(t.data) }} · equipe {{ t.equipe }}</span></div>
          }
        </div>
      }
      @if (l.devolvidas.length) { <p class="nota">Voltaram ao armazém: {{ codigos(l.devolvidas) }}</p> }

      <div class="laudo-previa">
        <header>
          <h3>Laudos</h3>
          <div role="group" aria-label="Qual laudo">
            <button mat-button type="button" [class.ativo]="tipoLaudo() === 'lote'" [attr.aria-pressed]="tipoLaudo() === 'lote'" (click)="tipoLaudo.set('lote')">Do lote (LD)</button>
            @if (l.status !== 'Pedido' && l.status !== 'Reprovado') {
              <button mat-button type="button" [class.ativo]="tipoLaudo() === 'liberacao-lote'" [attr.aria-pressed]="tipoLaudo() === 'liberacao-lote'" (click)="tipoLaudo.set('liberacao-lote')">Liberação (LB)</button>
            }
          </div>
        </header>
        <app-previa-laudo [tipo]="tipoLaudo()" [id]="l.id" />
      </div>
    </section>
  `,
})
export class LoteAberto {
  readonly lote = input.required<LoteTrocaDto>();
  /** O lote mudou: a mensagem para a tela (e ela busca o lote e a lista de novo). */
  readonly mudou = output<string>();
  readonly fechar = output<void>();
  private readonly api = inject(LotesTroca);
  private readonly sessao = inject(Sessao);
  protected readonly podeAprovar = computed(() => this.sessao.temCargo('Supervisor', 'Tecnico'));
  protected readonly ehTecnico = computed(() => this.sessao.temCargo('Tecnico'));
  protected readonly rotulo = ROTULO_LOTE;
  protected readonly km = km;
  protected readonly metros = metros;
  protected readonly dataHora = dataHora;

  readonly quantidade = linkedSignal(() => this.lote().quantidadePedida);
  readonly motivo = signal('');
  readonly urgente = signal(false);
  readonly resposta = signal('');
  protected readonly menos = computed(() => (this.quantidade() ?? 0) < this.lote().quantidadePedida);
  protected readonly tipoLaudo = linkedSignal<number, TipoLaudo>({ source: () => this.lote().id, computation: () => 'lote' });
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);
  protected readonly codigosAvulsas = computed(() => this.codigos(this.lote().avulsasAbertas));

  protected codigos(pecas: { codigo: string }[]): string {
    return pecas.map((p) => p.codigo).join(', ');
  }

  protected decidir(aprovar: boolean): void {
    const l = this.lote();
    const [quantidade, motivo] = [Number(this.quantidade()), this.motivo().trim()];
    const falta = !aprovar ? (motivo ? null : 'Para reprovar, escreva o motivo.')
      : !quantidade || quantidade < 1 || quantidade > l.quantidadePedida ? `A quantidade aprovada vai de 1 a ${l.quantidadePedida}.`
      : quantidade < l.quantidadePedida && !motivo ? 'Para aprovar menos peças que as pedidas, escreva o porquê.'
      : null;
    if (falta) {
      this.erro.set(falta);
      return;
    }
    this.enviar(this.api.decidir(l.id, aprovar, aprovar ? quantidade : null, motivo || null, aprovar && this.urgente()),
      aprovar ? `Lote aprovado com ${quantidade} peças. O pátio já pode separar.` : 'Lote reprovado.');
  }

  protected decidirRecusa(decisao: 'Manter' | 'Aceitar'): void {
    if (decisao === 'Manter' && !this.resposta().trim()) {
      this.erro.set('Para manter o lote, responda à via.');
      return;
    }
    this.enviar(this.api.decidirRecusa(this.lote().id, decisao, this.resposta().trim() || null),
      decisao === 'Manter' ? 'Lote mantido: volta para a via trocar.' : 'Recusa aceita: o lote foi cancelado e as peças voltaram ao armazém.');
  }

  private enviar(acao: Observable<unknown>, ok: string): void {
    this.enviando.set(true);
    this.erro.set(null);
    acao.subscribe({
      next: () => {
        this.enviando.set(false);
        this.mudou.emit(ok);
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }
}
