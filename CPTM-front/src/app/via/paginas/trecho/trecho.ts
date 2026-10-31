import { Component, ElementRef, computed, effect, inject, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { from, of } from 'rxjs';
import { km, mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { FaixaDoTrechoDto, MapaDoTrechoDto, PecaNoTrechoDto, Trechos, faixasDoTrecho } from '../../servicos/trechos';
import { TrabalhoDaNoite } from '../../../nucleo/ao-vivo/trabalho-da-noite';
import { AvisoGuardado } from '../../../nucleo/ao-vivo/aviso-guardado';
import { CopiaDaLinha } from '../../../nucleo/linha/copia-da-linha';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { Nota } from '../../../compartilhado/componentes/nota/nota';
import { FORMULARIO } from '../../../compartilhado/ui';
import { CartoesNoCelular } from '../../../compartilhado/componentes/tabela/cartoes';
import { valorDe } from '../../../compartilhado/utilitarios/recurso';

/** A pior nota primeiro: é ela que pinta a faixa e escolhe a peça aberta na lupa. */
const ORDEM: (string | null)[] = ['Crítico', 'Ruim', 'Regular', 'Bom', null];
const COR: Record<string, string> = { 'Crítico': 'var(--vermelho)', Ruim: 'var(--nota-ruim)', Regular: 'var(--areia)', Bom: 'var(--nota-bom)' };
const cor = (nota: string | null): string => (nota && COR[nota]) || 'var(--linha-2)';
const quantas = (f: FaixaDoTrechoDto, nota: string | null): number =>
  nota === 'Crítico' ? f.critico : nota === 'Ruim' ? f.ruim : nota === 'Regular' ? f.regular : nota === 'Bom' ? f.bom : f.semNota;
/** Cada faixa tem pelo menos 30 px (o selo cabe), 44 px no celular (cabe o dedo); com mais faixas que isto, a via rola para o lado. */
const FAIXAS_QUE_CABEM_NO_CELULAR = 7;
const FAIXAS_QUE_CABEM_NO_COMPUTADOR = 30;

/**
 * A tela do trecho (pedido do Victor em 2026-10-12): o trecho desenhado como a via, com os trilhos e os dormentes. Desde 2026-10-31
 * (Victor escolheu o "trilho pintado"), cada faixa de km com peça assentada é pintada com a pior nota e leva um selo com o número de
 * peças; quando a via não cabe na tela (no celular, com faixa de 100 m), ela rola para o lado e abre na pior faixa. Embaixo, a lupa da faixa escolhida mostra cada peça com tag no lugar dela, e a tabela
 * lista as peças com o ID de posição (L09-213100-0012 = linha · km · nº da peça no trecho).
 */
@Component({
  selector: 'app-trecho',
  imports: [AvisoGuardado, CartoesNoCelular, FORMULARIO, RouterLink, Estado, Nota],
  template: `
    <p class="sobre">Campo</p>
    <h1>Mapa do trecho</h1>
    <p class="rotulo">Cada pedaço pintado da via é uma faixa com peça, na cor da pior nota; o número é quantas peças tem. Toque num pedaço para ver as peças.</p>
    <app-aviso-guardado texto="Sem rede: o mapa sai do que está guardado no celular (as peças com tag, da cópia das linhas)" />
    <app-estado [carregando]="trechos.isLoading()" [erro]="erro()" [vazio]="trechos.hasValue() && !trechos.value().length" textoVazio="Nenhum trecho cadastrado." />
    @if (valorDe(trechos)?.length) {
      <div class="campos">
        <mat-form-field appearance="outline">
          <mat-label>Trecho</mat-label>
          <mat-select [value]="idAtual()" (selectionChange)="idTrecho.set($event.value)"><app-busca-opcoes />
            @for (t of valorDe(trechos) ?? []; track t.id) {
              <mat-option [value]="t.id">{{ t.nome }} · {{ t.pecas }} {{ t.pecas === 1 ? 'peça' : 'peças' }}{{ t.ruimOuCritico ? ' · ' + t.ruimOuCritico + ' ruim/crítico' : '' }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Faixa</mat-label>
          <mat-select [value]="faixaM()" (selectionChange)="faixaM.set($event.value)"><app-busca-opcoes />
            <mat-option [value]="100">100 m</mat-option><mat-option [value]="500">500 m</mat-option><mat-option [value]="1000">1 km</mat-option>
          </mat-select>
        </mat-form-field>
      </div>
    }
    @if (mapa.isLoading()) { <app-estado esqueleto="pagina" [carregando]="true" /> }
    @if (valorDe(mapa); as m) {
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
      <div class="cartao">
        <!-- a via tem largura para o selo de toda faixa; quando não cabe na tela, ela rola para o lado (Victor, 2026-10-31) -->
        <div class="via-rolagem">
          <div class="via" role="group" aria-label="A via do trecho, por faixa" [style.--n]="m.faixas.length">
            @for (f of pintadas(); track f.kmInicio) {
              <button type="button" class="via__faixa" [class.ativa]="escolhida() === f" (click)="escolhida.set(f)" [attr.aria-pressed]="escolhida() === f"
                [attr.aria-label]="rotulo(f)" [title]="rotulo(f)" [style.--ini]="posicao(f.kmInicio)" [style.--tam]="largura(f)" [style.--cor]="cor(pior(f))">
                <span class="via__qtd">{{ f.pecas.length }}</span>
              </button>
            }
            <span class="dormentes"></span><span class="trilho"></span><span class="trilho trilho--b"></span>
            @for (k of marcas(); track k; let p = $first; let u = $last; let i = $index) {
              <span class="via__km mono" [class.primeiro]="p" [class.ultimo]="u" [class.impar]="i % 2 === 1 && !u && m.faixas.length <= CABEM_NO_CELULAR" [style.--pos]="posicao(k)"><span class="via__km-nome">km </span>{{ kmCurto(k) }}</span>
            }
          </div>
        </div>
        @if (!pintadas().length) { <p class="rotulo">Nenhuma peça assentada neste trecho.</p> }
        <p class="via__dica" [class.no-celular]="m.faixas.length > CABEM_NO_CELULAR" [class.no-computador]="m.faixas.length > CABEM_NO_COMPUTADOR">Arraste a via para o lado →</p>
      </div>

      @if (escolhida(); as f) {
        <div class="titulo-lupa">
          <h2>Km {{ km(f.kmInicio) }} a {{ km(f.kmFim) }}</h2>
          <span class="rotulo">{{ f.pecas.length }} {{ f.pecas.length === 1 ? 'peça' : 'peças' }}@for (r of resumo(f); track r.nome) { · <span [class.vermelho]="r.alerta">{{ r.texto }}</span> }</span>
        </div>
        @if (f.pecas.length) {
          <div class="cartao lupa">
            <!-- a lupa tem escala fixa (6 px por metro): as peças não se amontoam, e ela rola para o lado quando passa da tela -->
            <div class="lupa-rolagem">
              <div class="lupa__largura" [style.--metros]="(f.kmFim - f.kmInicio) * 1000">
                <div class="lupa__via" role="group" [attr.aria-label]="'As peças do km ' + km(f.kmInicio) + ' a ' + km(f.kmFim)">
                  @for (p of f.pecas; track p.id) {
                    <button type="button" class="lupa__peca" [class.ativa]="pecaAberta() === p" (click)="pecaAberta.set(p)" [attr.aria-pressed]="pecaAberta() === p"
                      [attr.aria-label]="'Peça ' + (p.posicao ?? p.codigo) + ', km ' + km(p.km) + ', ' + (p.nota ?? 'sem nota')" [title]="(p.posicao ?? p.codigo) + ' · ' + (p.nota ?? 'sem nota')"
                      [style.--pos]="(p.km - f.kmInicio) / (f.kmFim - f.kmInicio)" [style.--cor]="cor(p.nota)"></button>
                  }
                  <span class="dormentes"></span><span class="trilho"></span><span class="trilho trilho--b"></span>
                </div>
                <p class="lupa__km mono">
                  @for (k of marcasDaLupa(); track k; let p = $first; let u = $last) {
                    <span [class.primeiro]="p" [class.ultimo]="u" [style.--pos]="(k - f.kmInicio) / (f.kmFim - f.kmInicio)">{{ km(k) }}</span>
                  }
                </p>
              </div>
            </div>
            <p class="via__dica no-celular" [class.no-computador]="faixaM() >= 500">Arraste a lupa para o lado →</p>
            @if (pecaAberta(); as p) {
              <p class="lupa__aberta">
                <app-nota [nota]="p.nota" /><span class="mono">{{ p.posicao ?? '—' }}</span><span class="mono rotulo">{{ p.codigo }} · km {{ km(p.km) }}</span>
                @if (p.tag) { <a [routerLink]="['/pecas']" [queryParams]="{ tag: p.tag }">Abrir a peça</a> }
              </p>
            }
          </div>
        }
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
    .via, .lupa__via { position: relative; isolation: isolate; }
    .via { height: 112px; }
    .dormentes, .trilho { position: absolute; z-index: 2; pointer-events: none; left: 0; right: 0; }
    .dormentes { top: 22px; height: 46px; background: repeating-linear-gradient(90deg, transparent 0 9px, var(--superficie-3) 9px 13px, transparent 13px 22px); }
    .trilho { top: 31px; height: 3px; border-radius: 2px; background: var(--tinta-3); }
    .trilho--b { top: 56px; }
    .via__faixa { position: absolute; top: 22px; height: 46px; left: calc(var(--ini) * 100% + 1px); width: max(4px, calc(var(--tam) * 100% - 2px));
      padding: 0; border: 0; border-radius: 8px; background: var(--cor); color: var(--tinta); font: inherit; cursor: pointer; }
    .via__faixa.ativa, .lupa__peca.ativa { outline: 2px solid var(--tinta); outline-offset: 3px; }
    .via__qtd { position: absolute; z-index: 3; left: 50%; top: 50%; transform: translate(-50%, -50%); display: grid; place-items: center; min-width: 26px; height: 26px;
      padding: 0 6px; border: 2px solid var(--cor); border-radius: 999px; background: var(--superficie); font: 700 12px/1 var(--fonte-mono); white-space: nowrap; }
    .via-rolagem { overflow-x: auto; overscroll-behavior-x: contain; }
    .via { min-width: calc(var(--n) * 30px); }
    .via__dica { display: none; margin: 4px 0 0; font-size: 12.5px; color: var(--tinta-3); text-align: right; }
    .via__dica.no-computador { display: block; }
    .via__km { position: absolute; top: 84px; left: calc(var(--pos) * 100%); transform: translateX(-50%); font-size: 12px; color: var(--tinta-3); white-space: nowrap; }
    .via__km.primeiro { transform: none; }
    .via__km.ultimo { transform: translateX(-100%); }
    .titulo-lupa { display: flex; flex-wrap: wrap; align-items: baseline; gap: 4px 12px; margin: 20px 0 8px; }
    .titulo-lupa h2 { margin: 0; }
    .lupa { margin-bottom: 12px; }
    /* só para o lado: sem isto, o contorno da peça aberta sobrava 1 px e o computador mostrava uma barra de rolagem em pé (Victor, 2026-10-31) */
    .via-rolagem, .lupa-rolagem { overflow-y: hidden; }
    .lupa-rolagem { overflow-x: auto; overscroll-behavior-x: contain; padding-top: 3px; }
    .lupa__largura { min-width: min(2400px, calc(var(--metros) * 6px)); }
    .lupa__via { height: 80px; }
    .lupa__via .dormentes { top: 8px; height: 64px; background: repeating-linear-gradient(90deg, transparent 0 3px, var(--superficie-3) 3px 6px, transparent 6px 8px); }
    .lupa__via .trilho { top: 22px; }
    .lupa__via .trilho--b { top: 55px; }
    .lupa__peca { position: absolute; z-index: 3; top: 4px; height: 72px; width: 8px; left: calc(var(--pos) * (100% - 8px)); padding: 0; border: 0; border-radius: 4px;
      background: var(--cor); cursor: pointer; }
    .lupa__peca::before { content: ''; position: absolute; inset: -4px -8px; }
    .lupa__km { position: relative; height: 16px; margin: 6px 0 0; font-size: 11.5px; color: var(--tinta-3); }
    .lupa__km span { position: absolute; left: calc(var(--pos) * 100%); transform: translateX(-50%); white-space: nowrap; }
    .lupa__km .primeiro { transform: none; }
    .lupa__km .ultimo { transform: translateX(-100%); }
    .lupa__aberta { display: flex; flex-wrap: wrap; align-items: center; gap: 6px 12px; margin: 12px 0 0; font-size: 14px; }
    /* No celular, a via fica deitada como no computador e rola para o lado, nunca para baixo (Victor, 2026-10-31). */
    @media (max-width: 600px) {
      .via { min-width: calc(var(--n) * 44px); }
      .via__km { font-size: 11px; }
      .via__km.impar, .via__km-nome { display: none; }
      .via__dica.no-celular { display: block; }
    }
  `,
})
export class Trecho {
  protected readonly valorDe = valorDe;
  private readonly api = inject(Trechos);
  private readonly trabalho = inject(TrabalhoDaNoite);
  private readonly copia = inject(CopiaDaLinha);
  protected readonly km = km;
  protected readonly cor = cor;
  protected readonly CABEM_NO_CELULAR = FAIXAS_QUE_CABEM_NO_CELULAR;
  protected readonly CABEM_NO_COMPUTADOR = FAIXAS_QUE_CABEM_NO_COMPUTADOR;
  private readonly tela = inject<ElementRef<HTMLElement>>(ElementRef);
  // Sem rede (no app): os trechos do pacote guardado.
  protected readonly trechos = rxResource({
    params: () => ({ guardado: this.trabalho.usarGuardado() }),
    stream: ({ params }) => (params.guardado ? of(this.trabalho.pacote()?.trechos ?? []) : this.api.listar()),
  });
  protected readonly erro = computed(() => (this.trechos.error() ? mensagemDeErro(this.trechos.error()) : null));
  protected readonly idTrecho = signal<number | null>(null);
  protected readonly faixaM = signal(100);
  /** o escolhido; sem escolha, o primeiro trecho com peça assentada */
  protected readonly idAtual = computed(() => {
    const lista = valorDe(this.trechos) ?? [];
    return this.idTrecho() ?? (lista.find((t) => t.pecas > 0) ?? lista[0])?.id ?? null;
  });
  protected readonly mapa = rxResource({
    params: () => (this.idAtual() === null ? undefined : { id: this.idAtual()!, faixaM: this.faixaM(), guardado: this.trabalho.usarGuardado() }),
    stream: ({ params }) => (params.guardado ? from(this.mapaGuardado(params.id, params.faixaM)) : this.api.mapa(params.id, params.faixaM)),
  });
  /** a faixa aberta: começa pela pior (crítico, depois ruim) ou pela primeira com peça */
  protected readonly escolhida = linkedSignal<FaixaDoTrechoDto | null>(() => {
    const faixas = valorDe(this.mapa)?.faixas ?? [];
    return faixas.find((f) => f.critico) ?? faixas.find((f) => f.ruim) ?? faixas.find((f) => f.pecas.length) ?? null;
  });
  /** a peça aberta na lupa: começa pela pior da faixa */
  protected readonly pecaAberta = linkedSignal<PecaNoTrechoDto | null>(() => {
    const pecas = this.escolhida()?.pecas ?? [];
    return ORDEM.map((nota) => pecas.find((p) => p.nota === nota)).find((p) => p) ?? null;
  });
  /** só as faixas com peça são pintadas */
  protected readonly pintadas = computed(() => (valorDe(this.mapa)?.faixas ?? []).filter((f) => f.pecas.length));

  constructor() {
    // quando a via (ou a lupa) rola para o lado, a faixa aberta (ou a peça aberta) aparece no meio da tela: a primeira vez vai
    // direto, depois de cada toque desliza. Quando o mapa chega, a via ainda não tem largura no primeiro quadro: tenta de novo nos
    // seguintes (até ~0,5 s).
    const jaRolou = new Set<string>();
    const centralizar = (rolagem: string, aberta: string, tentativas = 30): void => {
      requestAnimationFrame(() => {
        const caixa = this.tela.nativeElement.querySelector<HTMLElement>(rolagem);
        const ativa = caixa?.querySelector<HTMLElement>(aberta);
        if (!caixa || !ativa) return;
        if (!caixa.clientWidth) return void (tentativas && centralizar(rolagem, aberta, tentativas - 1));
        if (caixa.scrollWidth <= caixa.clientWidth) return;
        caixa.scrollTo({ left: ativa.offsetLeft + ativa.offsetWidth / 2 - caixa.clientWidth / 2, behavior: jaRolou.has(rolagem) ? 'smooth' : 'instant' });
        jaRolou.add(rolagem);
      });
    };
    effect(() => {
      if (this.escolhida()) centralizar('.via-rolagem', '.via__faixa.ativa');
    });
    effect(() => {
      if (this.pecaAberta()) centralizar('.lupa-rolagem', '.lupa__peca.ativa');
    });
  }
  /** de onde a onde vai a via desenhada: da primeira à última faixa */
  private readonly ponta = computed(() => {
    const faixas = valorDe(this.mapa)?.faixas ?? [];
    return faixas.length ? { inicio: faixas[0].kmInicio, fim: faixas[faixas.length - 1].kmFim } : { inicio: 0, fim: 1 };
  });
  /** a régua: no máximo 7 marcas, de 100 m, 200 m, 500 m, 1 km… */
  protected readonly marcas = computed(() => {
    const { inicio, fim } = this.ponta();
    const passo = [0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50].find((p) => (fim - inicio) / p <= 6) ?? 100;
    const lista: number[] = [];
    for (let k = Math.ceil(inicio / passo - 1e-9) * passo; k <= fim + 1e-9; k += passo) lista.push(+k.toFixed(3));
    return lista;
  });
  /** a régua da lupa: a lupa tem 6 px por metro (no máximo 2.400 px); uma marca a cada 10, 25, 50… m, com pelo menos 90 px entre elas */
  protected readonly marcasDaLupa = computed(() => {
    const f = this.escolhida();
    if (!f) return [];
    const metros = (f.kmFim - f.kmInicio) * 1000, escala = Math.min(6, 2400 / metros);
    const passo = ([10, 25, 50, 100, 250, 500].find((p) => p * escala >= 90) ?? 1000) / 1000;
    const lista: number[] = [];
    for (let k = f.kmInicio; k <= f.kmFim + 1e-9; k += passo) lista.push(+k.toFixed(3));
    return lista;
  });

  /** Sem rede: o resumo do trecho vem do pacote guardado e as faixas, das peças da cópia das linhas. */
  private async mapaGuardado(id: number, faixaM: number): Promise<MapaDoTrechoDto | null> {
    const trecho = this.trabalho.pacote()?.trechos.find((t) => t.id === id);
    if (!trecho) return null;
    const pecas = (await this.copia.pecasDoTrecho(id))
      .filter((p) => p.km !== null)
      .map((p) => ({ id: p.id, codigo: p.codigo, tag: p.tag, posicao: p.posicao, km: p.km!, nota: p.nota }));
    return { trecho, faixaM, faixas: faixasDoTrecho(trecho.kmInicio, trecho.kmFim, faixaM, pecas) };
  }

  /** onde o km cai na via desenhada, de 0 a 1 */
  protected posicao(valor: number): number {
    const { inicio, fim } = this.ponta();
    return (valor - inicio) / (fim - inicio || 1);
  }

  protected largura(f: FaixaDoTrechoDto): number {
    return this.posicao(f.kmFim) - this.posicao(f.kmInicio);
  }

  protected pior(f: FaixaDoTrechoDto): string | null {
    return ORDEM.find((nota) => quantas(f, nota) > 0) ?? null;
  }

  protected resumo(f: FaixaDoTrechoDto): { nome: string; texto: string; alerta: boolean }[] {
    return ORDEM.filter((nota) => quantas(f, nota)).map((nota) => ({
      nome: nota ?? 'sem nota',
      texto: `${quantas(f, nota)} ${(nota ?? 'sem nota').toLowerCase()}`,
      alerta: nota === 'Crítico' || nota === 'Ruim',
    }));
  }

  /** o km da régua, com uma casa: 212,5 */
  protected kmCurto(valor: number): string {
    return valor.toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 });
  }

  protected rotulo(f: FaixaDoTrechoDto): string {
    const partes = [['Bom', f.bom], ['Regular', f.regular], ['Ruim', f.ruim], ['Crítico', f.critico], ['sem nota', f.semNota]]
      .filter(([, n]) => n)
      .map(([nome, n]) => `${n} ${nome}`);
    return `km ${km(f.kmInicio)} a ${km(f.kmFim)}: ${partes.length ? partes.join(', ') : 'nenhuma peça'}`;
  }
}
