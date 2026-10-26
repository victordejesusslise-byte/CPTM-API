import { BreakpointObserver } from '@angular/cdk/layout';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { Sessao } from '../../sessao/sessao';
import { Tema } from '../tema/tema';
import { ItemMenu, SubMenu, menuDoCargo } from '../menu/menu';
import { nomeDoCargo } from '../../../compartilhado/utilitarios/formato';
import { Marca } from '../../../compartilhado/componentes/marca/marca';
import { PainelLeitor } from '../../leitor/painel-leitor';
import { EstadoFila } from '../../fila/estado-fila';
import { AvisosDoApp } from '../../ao-vivo/avisos-do-app';
import { TrabalhoDaNoite } from '../../ao-vivo/trabalho-da-noite';
import { Cargo } from '../../../pessoas/entidades/usuario';

interface ItemVisivel extends ItemMenu {
  link: string;
  ativo: boolean;
  aberto: boolean;
  abas: SubMenu[];
}

/**
 * A moldura das telas logadas, como no protótipo YBYRA: barra de cima, menu do cargo e o conteúdo.
 * Tela com abas (Pedidos: Cadastrar e Acompanhar): no computador as abas abrem embaixo do item no menu;
 * no celular viram abas no alto. Aba "só computador" some no celular; uma aba sozinha não vira menu.
 */
/** Quanto tempo, no mínimo, o skeleton de página fica ao abrir uma tela. */
const MINIMO_DO_SKELETON_MS = 300;

/** A tela sem o ?… e o #…: abrir um item ou trocar um filtro não é abrir outra tela. */
const semConsulta = (url: string) => url.split(/[?#]/)[0];

/** As telas do trabalho da noite, que abrem sem rede no app (o resto precisa de rede). */
const FUNCIONAM_SEM_REDE: { rota: string; rotulo: string; cargos: Cargo[]; outras?: string[] }[] = [
  { rota: '/trocas-trecho', rotulo: 'Trocas do trecho', cargos: ['OperadorVia', 'Supervisor'] },
  { rota: '/inspecoes', rotulo: 'Fazer a inspeção', cargos: ['Tecnico'] },
  { rota: '/alertas', rotulo: 'Alertas (vistoria)', cargos: ['OperadorVia', 'Tecnico', 'Supervisor'], outras: ['/alertas/sensores'] },
];

/** O toque começou em algo que desliza para o lado (gráfico, tabela larga, campo)? Aí o deslize é dele, não do menu. */
const deslizaSozinho = (alvo: EventTarget | null): boolean => {
  for (let el = alvo instanceof Element ? alvo : null; el && el !== document.body; el = el.parentElement) {
    if (el.matches('input, textarea, select, [role="slider"]')) return true;
    if (el.scrollWidth > el.clientWidth + 2 && /(auto|scroll)/.test(getComputedStyle(el).overflowX)) return true;
  }
  return false;
};

@Component({
  selector: 'app-casca',
  imports: [Esqueleto, RouterOutlet, RouterLink, Marca, PainelLeitor, EstadoFila, AvisosDoApp],
  templateUrl: './casca.html',
  styleUrl: './casca.scss',
  host: { '(touchstart)': 'tocou($event)', '(touchend)': 'soltou($event)' },
})
export class Casca {
  private readonly sessao = inject(Sessao);
  private readonly router = inject(Router);

  protected readonly usuario = this.sessao.usuario;
  protected readonly cargo = computed(() => nomeDoCargo(this.usuario()?.cargo));
  protected readonly menuAberto = signal(false);
  protected readonly tema = inject(Tema);
  /** itens com abas que a pessoa recolheu clicando de novo neles */
  private readonly recolhidos = signal(new Set<string>());
  /**
   * Toda tela que abre mostra o skeleton de página por pelo menos 0,3 s (Victor, 2026-10-18: "skeleton curto ao abrir toda tela"),
   * e mais, se o pedaço dela ainda vier do servidor. Abrir um item ou trocar um filtro na mesma tela (só muda o ?…) não mostra.
   */
  protected readonly navegando = signal(true);
  private inicioDaTela = Date.now();
  private esperaDoSkeleton = setTimeout(() => this.navegando.set(false), MINIMO_DO_SKELETON_MS);
  protected readonly celular = toSignal(inject(BreakpointObserver).observe('(max-width: 820px)').pipe(map((r) => r.matches)), { initialValue: false });
  private readonly url = toSignal(
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd), map(() => this.router.url)),
    { initialValue: this.router.url },
  );
  protected readonly caminho = computed(() => this.url().split(/[?#]/)[0]);

  protected readonly grupos = computed(() => {
    const caminho = this.caminho();
    const itens: ItemVisivel[] = menuDoCargo(this.usuario()?.cargo).map((item) => {
      const cargo = this.usuario()?.cargo;
      const subs = (item.subs ?? []).filter((s) => !(s.soComputador && this.celular()) && !(s.cargos && cargo && !s.cargos.includes(cargo)));
      const abas = subs.length > 1 ? subs : [];
      const ativo = caminho === item.rota || caminho.startsWith(item.rota + '/');
      return { ...item, abas, ativo, aberto: ativo && abas.length > 0 && !this.recolhidos().has(item.rota), link: subs[0]?.rota ?? item.rota };
    });
    const grupos = new Map<string, ItemVisivel[]>();
    for (const item of itens) grupos.set(item.grupo, [...(grupos.get(item.grupo) ?? []), item]);
    return [...grupos].map(([nome, itensDoGrupo]) => ({ nome, itens: itensDoGrupo }));
  });
  private readonly trabalho = inject(TrabalhoDaNoite);
  /** No app e sem rede: as telas que funcionam sem rede para este cargo. */
  protected readonly telasSemRede = computed(() => FUNCIONAM_SEM_REDE.filter((t) => this.sessao.temCargo(...t.cargos)));
  /** No app e sem rede, nesta tela que precisa de rede: no lugar dela, o aviso com as telas que funcionam (a tela fica guardada por trás). */
  protected readonly precisaDeRede = computed(
    () => this.trabalho.usarGuardado() && !FUNCIONAM_SEM_REDE.some((t) => this.caminho() === t.rota || t.outras?.includes(this.caminho())),
  );
  private toque: { x: number; y: number; quando: number } | null = null;
  protected readonly abasNoAlto = computed(() => (this.celular() ? (this.grupos().flatMap((g) => g.itens).find((i) => i.ativo)?.abas ?? []) : []));

  constructor() {
    this.router.events.pipe(takeUntilDestroyed()).subscribe((e) => {
      if (e instanceof NavigationStart) {
        if (semConsulta(e.url) === semConsulta(this.router.url)) return; // a mesma tela
        clearTimeout(this.esperaDoSkeleton);
        this.inicioDaTela = Date.now();
        this.navegando.set(true);
      } else if ((e instanceof NavigationEnd || e instanceof NavigationCancel || e instanceof NavigationError) && this.navegando()) {
        clearTimeout(this.esperaDoSkeleton);
        const falta = Math.max(0, MINIMO_DO_SKELETON_MS - (Date.now() - this.inicioDaTela));
        this.esperaDoSkeleton = setTimeout(() => this.navegando.set(false), falta);
      }
    });
  }

  /** No item aberto, o clique recolhe ou abre as abas (sem sair da tela); nos outros, navega e abre. */
  protected clicar(item: ItemVisivel): void {
    const recolhidos = new Set(this.recolhidos());
    if (item.ativo && item.abas.length) {
      if (recolhidos.has(item.rota)) recolhidos.delete(item.rota);
      else recolhidos.add(item.rota);
    } else {
      recolhidos.delete(item.rota);
      this.menuAberto.set(false);
    }
    this.recolhidos.set(recolhidos);
  }

  /** No celular: deslizar o dedo muda de aba (esquerda: a próxima; direita: a anterior) e, na primeira aba, abre o menu; com o menu aberto, para a esquerda fecha (Victor, 2026-10-24 e 25). */
  protected tocou(e: TouchEvent): void {
    const dedo = e.touches[0];
    this.toque = this.celular() && e.touches.length === 1 && !deslizaSozinho(e.target) ? { x: dedo.clientX, y: dedo.clientY, quando: e.timeStamp } : null;
  }

  protected soltou(e: TouchEvent): void {
    const inicio = this.toque;
    this.toque = null;
    const dedo = e.changedTouches[0];
    if (!inicio || !dedo || e.timeStamp - inicio.quando > 700) return;
    const [dx, dy] = [dedo.clientX - inicio.x, dedo.clientY - inicio.y];
    // Um deslize de verdade: pelo menos 70 px para o lado e mais de lado do que para cima ou para baixo.
    if (Math.abs(dx) < 70 || Math.abs(dy) > Math.abs(dx) * 0.6) return;
    if (this.menuAberto()) {
      if (dx < 0) this.menuAberto.set(false);
      return;
    }
    // Pelas abas do alto (Victor, 2026-10-25): para a esquerda, a próxima aba; para a direita, a anterior; na primeira, abre o menu.
    const abas = this.abasNoAlto();
    const atual = abas.findIndex((a) => a.rota === this.caminho());
    if (dx < 0) {
      if (atual >= 0 && atual < abas.length - 1) void this.router.navigateByUrl(abas[atual + 1].rota);
    } else if (atual > 0) void this.router.navigateByUrl(abas[atual - 1].rota);
    else this.menuAberto.set(true);
  }

  protected sair(): void {
    this.sessao.encerrar().subscribe({ complete: () => this.router.navigate(['/login']) });
  }
}