import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Sessao } from '../../core/auth/sessao';
import { ItemMenu, menuDoCargo } from '../../core/menu/menu';
import { nomeDoCargo } from '../../helpers/formato';
import { Marca } from '../marca/marca';

/** A moldura das telas logadas: barra de cima com quem entrou, menu do cargo e o conteúdo. */
@Component({
  selector: 'app-casca',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Marca],
  templateUrl: './casca.html',
  styleUrl: './casca.scss',
})
export class Casca {
  private readonly sessao = inject(Sessao);
  private readonly router = inject(Router);

  protected readonly usuario = this.sessao.usuario;
  protected readonly cargo = computed(() => nomeDoCargo(this.usuario()?.cargo));
  protected readonly menuAberto = signal(false);
  protected readonly grupos = computed(() => {
    const itens = menuDoCargo(this.usuario()?.cargo);
    const grupos = new Map<string, ItemMenu[]>();
    for (const item of itens) grupos.set(item.grupo, [...(grupos.get(item.grupo) ?? []), item]);
    return [...grupos].map(([nome, itensDoGrupo]) => ({ nome, itens: itensDoGrupo }));
  });

  protected sair(): void {
    this.sessao.sair();
    this.router.navigate(['/login']);
  }
}