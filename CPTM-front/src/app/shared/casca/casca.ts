import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { Sessao } from '../../core/auth/sessao';
import { ItemMenu, SubMenu, menuDoCargo } from '../../core/menu/menu';
import { nomeDoCargo } from '../../helpers/formato';
import { Marca } from '../marca/marca';

interface ItemVisivel extends ItemMenu {
  link: string;
  ativo: boolean;
  abas: SubMenu[];
}

/**
 * A moldura das telas logadas, como no protótipo YBYRA: barra de cima, menu do cargo e o conteúdo.
 * Tela com abas (Pedidos: Cadastrar e Acompanhar): no computador as abas abrem embaixo do item no menu;
 * no celular viram abas no alto. Aba "só computador" some no celular; uma aba sozinha não vira menu.
 */
@Component({
  selector: 'app-casca',
  imports: [RouterOutlet, RouterLink, Marca],
  templateUrl: './casca.html',
  styleUrl: './casca.scss',
})
export class Casca {
  private readonly sessao = inject(Sessao);
  private readonly router = inject(Router);

  protected readonly usuario = this.sessao.usuario;
  protected readonly cargo = computed(() => nomeDoCargo(this.usuario()?.cargo));
  protected readonly menuAberto = signal(false);
  protected readonly celular = toSignal(inject(BreakpointObserver).observe('(max-width: 820px)').pipe(map((r) => r.matches)), { initialValue: false });
  private readonly url = toSignal(
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd), map(() => this.router.url)),
    { initialValue: this.router.url },
  );
  protected readonly caminho = computed(() => this.url().split(/[?#]/)[0]);

  protected readonly grupos = computed(() => {
    const caminho = this.caminho();
    const itens: ItemVisivel[] = menuDoCargo(this.usuario()?.cargo).map((item) => {
      const subs = (item.subs ?? []).filter((s) => !(s.soComputador && this.celular()));
      const abas = subs.length > 1 ? subs : [];
      const ativo = caminho === item.rota || caminho.startsWith(item.rota + '/');
      return { ...item, abas, ativo, link: subs[0]?.rota ?? item.rota };
    });
    const grupos = new Map<string, ItemVisivel[]>();
    for (const item of itens) grupos.set(item.grupo, [...(grupos.get(item.grupo) ?? []), item]);
    return [...grupos].map(([nome, itensDoGrupo]) => ({ nome, itens: itensDoGrupo }));
  });
  protected readonly abasNoAlto = computed(() => (this.celular() ? (this.grupos().flatMap((g) => g.itens).find((i) => i.ativo)?.abas ?? []) : []));

  protected sair(): void {
    this.sessao.sair();
    this.router.navigate(['/login']);
  }
}