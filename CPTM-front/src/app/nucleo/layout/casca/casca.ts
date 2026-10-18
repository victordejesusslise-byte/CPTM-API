import { BreakpointObserver } from '@angular/cdk/layout';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map, of, switchMap, timer } from 'rxjs';
import { Sessao } from '../../sessao/sessao';
import { Tema } from '../tema/tema';
import { ItemMenu, SubMenu, menuDoCargo } from '../menu/menu';
import { nomeDoCargo } from '../../../compartilhado/utilitarios/formato';
import { Marca } from '../../../compartilhado/componentes/marca/marca';

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
@Component({
  selector: 'app-casca',
  imports: [Esqueleto, RouterOutlet, RouterLink, Marca],
  templateUrl: './casca.html',
  styleUrl: './casca.scss',
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
  /** Uma tela nova está carregando (o pedaço dela vem do servidor na primeira vez): mostra o skeleton de página se passar de 150 ms. */
  protected readonly navegando = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationStart || e instanceof NavigationEnd || e instanceof NavigationCancel || e instanceof NavigationError),
      // Só se a tela demorar: troca rápida não pisca o skeleton.
      switchMap((e) => (e instanceof NavigationStart ? timer(150).pipe(map(() => true)) : of(false))),
    ),
    { initialValue: false },
  );
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
  protected readonly abasNoAlto = computed(() => (this.celular() ? (this.grupos().flatMap((g) => g.itens).find((i) => i.ativo)?.abas ?? []) : []));

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

  protected sair(): void {
    this.sessao.sair();
    this.router.navigate(['/login']);
  }
}