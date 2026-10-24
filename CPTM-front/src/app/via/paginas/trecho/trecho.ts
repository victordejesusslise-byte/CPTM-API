import { Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { km, mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { FaixaDoTrechoDto, Trechos } from '../../servicos/trechos';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { Nota } from '../../../compartilhado/componentes/nota/nota';
import { FORMULARIO } from '../../../compartilhado/ui';
import { CartoesNoCelular } from '../../../compartilhado/componentes/tabela/cartoes';

/**
 * A tela do trecho (pedido do Victor em 2026-10-12): cada barra é uma faixa de km com a nota das peças assentadas nela;
 * tocar na barra lista as peças com o ID de posição (L09-213100-0012 = linha · km · nº da peça no trecho).
 */
@Component({
  selector: 'app-trecho',
  imports: [CartoesNoCelular, FORMULARIO, RouterLink, Estado, Nota],
  template: `
    <p class="sobre">Campo</p>
    <h1>Mapa do trecho</h1>
    <p class="rotulo">Cada barra é uma faixa de km; a cor é a nota das peças assentadas nela. Toque numa barra para ver as peças.</p>
    <app-estado [carregando]="trechos.isLoading()" [erro]="erro()" [vazio]="trechos.hasValue() && !trechos.value().length" textoVazio="Nenhum trecho cadastrado." />
    @if (trechos.value()?.length) {
      <div class="campos">
        <mat-form-field appearance="outline">
          <mat-label>Trecho</mat-label>
          <mat-select [value]="idAtual()" (selectionChange)="idTrecho.set($event.value)">
            @for (t of trechos.value(); track t.id) {
              <mat-option [value]="t.id">{{ t.nome }} · {{ t.pecas }} {{ t.pecas === 1 ? 'peça' : 'peças' }}{{ t.ruimOuCritico ? ' · ' + t.ruimOuCritico + ' ruim/crítico' : '' }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Faixa</mat-label>
          <mat-select [value]="faixaM()" (selectionChange)="faixaM.set($event.value)">
            <mat-option [value]="100">100 m</mat-option><mat-option [value]="500">500 m</mat-option><mat-option [value]="1000">1 km</mat-option>
          </mat-select>
        </mat-form-field>
      </div>
    }
    @if (mapa.isLoading()) { <app-estado esqueleto="pagina" [carregando]="true" /> }
    @if (mapa.value(); as m) {
      <div class="kv cartao">
        <div><span class="lbl">Linha</span><b>{{ m.trecho.linha }}</b></div>
        <div><span class="lbl">Km</span><b class="mono">{{ km(m.trecho.kmInicio) }} a {{ km(m.trecho.kmFim) }}</b></div>
        <div><span class="lbl">Peças assentadas</span><b>{{ m.trecho.pecas }}</b></div>
        <div><span class="lbl">Ruim ou crítico</span><b [class.vermelho]="m.trecho.ruimOuCritico > 0">{{ m.trecho.ruimOuCritico }}</b></div>
      </div>
      <p class="legenda">
        <span class="pilula nota-peca nota-peca--bom">Bom</span> <span class="pilula nota-peca nota-peca--regular">Regular</span>
        <span class="pilula nota-peca nota-peca--ruim">Ruim</span> <span class="pilula nota-peca nota-peca--critico">Crítico</span>
        <span class="pilula pilula--quieta">sem nota</span>
      </p>
      <div class="mapa-trecho" role="group" aria-label="Faixas do trecho">
        @for (f of m.faixas; track f.kmInicio) {
          <button type="button" class="mapa-trecho__faixa" [class.ativa]="escolhida() === f" (click)="escolhida.set(f)"
            [attr.aria-pressed]="escolhida() === f" [attr.aria-label]="rotulo(f)" [title]="rotulo(f)">
            <span class="mapa-trecho__pilha" [style.height.%]="altura(f)">
              @if (f.bom) { <span class="seg seg--bom" [style.flex-grow]="f.bom"></span> }
              @if (f.regular) { <span class="seg seg--regular" [style.flex-grow]="f.regular"></span> }
              @if (f.ruim) { <span class="seg seg--ruim" [style.flex-grow]="f.ruim"></span> }
              @if (f.critico) { <span class="seg seg--critico" [style.flex-grow]="f.critico"></span> }
              @if (f.semNota) { <span class="seg seg--sem" [style.flex-grow]="f.semNota"></span> }
            </span>
          </button>
        }
      </div>
      <p class="mapa-trecho__eixo mono"><span>km {{ km(m.faixas[0].kmInicio) }}</span><span>km {{ km(m.faixas[m.faixas.length - 1].kmFim) }}</span></p>

      @if (escolhida(); as f) {
        <h2>Km {{ km(f.kmInicio) }} a {{ km(f.kmFim) }}</h2>
        <div class="tabela"><table class="simples">
          <thead><tr><th>Posição</th><th>Peça</th><th class="num">Km</th><th>Nota</th><th></th></tr></thead>
          <tbody>@for (p of f.pecas; track p.id) {
            <tr>
              <td class="mono">{{ p.posicao ?? '—' }}</td>
              <td class="mono">{{ p.codigo }}</td>
              <td class="num">{{ km(p.km) }}</td>
              <td><app-nota [nota]="p.nota" /></td>
              <td>@if (p.tag) { <a [routerLink]="['/pecas']" [queryParams]="{ tag: p.tag }">Abrir a peça</a> }</td>
            </tr>
          } @empty { <tr><td colspan="5">Nenhuma peça assentada nesta faixa.</td></tr> }</tbody>
        </table></div>
      }
    }
  `,
  styles: `
    .legenda { display: flex; flex-wrap: wrap; gap: 6px; margin: 14px 0 8px; }
    .mapa-trecho { display: flex; align-items: flex-end; gap: 2px; height: 180px; padding: 8px; overflow-x: auto;
      border: 1px solid var(--linha); border-radius: 4px; background: var(--superficie); }
    .mapa-trecho__faixa { flex: 1 0 22px; min-width: 22px; height: 100%; display: flex; align-items: flex-end; padding: 0; cursor: pointer;
      border: 0; border-bottom: 2px solid var(--linha-2); background: transparent; }
    .mapa-trecho__faixa:hover, .mapa-trecho__faixa.ativa { background: var(--superficie-3); }
    .mapa-trecho__faixa:focus-visible { outline: 2px solid var(--anel); }
    .mapa-trecho__pilha { width: 100%; display: flex; flex-direction: column-reverse; }
    .seg { display: block; min-height: 4px; }
    .seg--bom { background: var(--nota-bom); }
    .seg--regular { background: var(--areia); }
    .seg--ruim { background: var(--madeira); }
    .seg--critico { background: var(--vermelho); }
    .seg--sem { background: var(--linha-2); }
    .mapa-trecho__eixo { display: flex; justify-content: space-between; font-size: 12px; color: var(--tinta-3); margin: 4px 0 0; }
  `,
})
export class Trecho {
  private readonly api = inject(Trechos);
  protected readonly km = km;
  protected readonly trechos = rxResource({ stream: () => this.api.listar() });
  protected readonly erro = computed(() => (this.trechos.error() ? mensagemDeErro(this.trechos.error()) : null));
  protected readonly idTrecho = signal<number | null>(null);
  protected readonly faixaM = signal(100);
  /** o escolhido; sem escolha, o primeiro trecho com peça assentada */
  protected readonly idAtual = computed(() => {
    const lista = this.trechos.value() ?? [];
    return this.idTrecho() ?? (lista.find((t) => t.pecas > 0) ?? lista[0])?.id ?? null;
  });
  protected readonly mapa = rxResource({
    params: () => (this.idAtual() === null ? undefined : { id: this.idAtual()!, faixaM: this.faixaM() }),
    stream: ({ params }) => (params ? this.api.mapa(params.id, params.faixaM) : of(null)),
  });
  /** a faixa aberta: começa pela pior (crítico, depois ruim) ou pela primeira com peça */
  protected readonly escolhida = linkedSignal<FaixaDoTrechoDto | null>(() => {
    const faixas = this.mapa.value()?.faixas ?? [];
    return faixas.find((f) => f.critico) ?? faixas.find((f) => f.ruim) ?? faixas.find((f) => f.pecas.length) ?? null;
  });
  private readonly maior = computed(() => Math.max(1, ...(this.mapa.value()?.faixas ?? []).map((f) => f.pecas.length)));

  protected altura(f: FaixaDoTrechoDto): number {
    return (f.pecas.length / this.maior()) * 100;
  }

  protected rotulo(f: FaixaDoTrechoDto): string {
    const partes = [['Bom', f.bom], ['Regular', f.regular], ['Ruim', f.ruim], ['Crítico', f.critico], ['sem nota', f.semNota]]
      .filter(([, n]) => n)
      .map(([nome, n]) => `${n} ${nome}`);
    return `km ${km(f.kmInicio)} a ${km(f.kmFim)}: ${partes.length ? partes.join(', ') : 'nenhuma peça'}`;
  }
}
