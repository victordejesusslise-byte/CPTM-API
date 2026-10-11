import { Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Sessao } from '../../core/auth/sessao';
import { menuDoCargo } from '../../core/menu/menu';
import { mensagemDeErro } from '../../helpers/formato';
import { IndicadorDto } from '../../models/indicador';
import { Indicadores } from '../../services/indicadores';
import { Estado } from '../../shared/estado/estado';

/** A primeira tela: atalhos do cargo e os indicadores (vermelho só no que pede ação). */
@Component({
  selector: 'app-painel',
  imports: [RouterLink, Estado],
  templateUrl: './painel.html',
  styleUrl: './painel.scss',
})
export class Painel {
  /** O que fazer hoje (atalhos) ou Acompanhamento (indicadores): vem do data da rota */
  readonly aba = input<'hoje' | 'numeros'>('hoje');
  private readonly sessao = inject(Sessao);
  private readonly servico = inject(Indicadores);

  protected readonly usuario = this.sessao.usuario;
  protected readonly atalhos = computed(() => menuDoCargo(this.usuario()?.cargo).filter((i) => i.rota !== '/painel'));
  protected readonly indicadores = rxResource({ stream: () => this.servico.listar() });
  protected readonly erro = computed(() => (this.indicadores.error() ? mensagemDeErro(this.indicadores.error()) : null));
  protected readonly noVermelho = computed(() => (this.indicadores.value() ?? []).filter((i) => i.vermelho).length);

  protected valor(i: IndicadorDto): string {
    if (i.valor === null) return '—';
    return i.valor.toLocaleString('pt-BR', { maximumFractionDigits: 2 });
  }
}