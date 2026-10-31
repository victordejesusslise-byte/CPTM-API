import { Component, computed, inject, input } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { Cargo } from '../../../pessoas/entidades/usuario';
import { VERIFICAR, Verificacao } from './verificar';
import { Sessao } from '../../../nucleo/sessao/sessao';
import { menuDoCargo } from '../../../nucleo/layout/menu/menu';
import { mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { IndicadorDto } from '../../entidades/indicador';
import { Apresentacao } from '../../../apresentacao/servicos/apresentacao';
import { Indicadores } from '../../servicos/indicadores';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { BOTOES } from '../../../compartilhado/ui';
import { valorDe } from '../../../compartilhado/utilitarios/recurso';

/** A primeira tela: atalhos do cargo e os indicadores. O que pede atenção vem em vermelho, em destaque e primeiro, com o botão para a tela onde se verifica. */
@Component({
  selector: 'app-painel',
  imports: [Esqueleto, RouterLink, Estado, BOTOES],
  templateUrl: './painel.html',
  styleUrl: './painel.scss',
})
export class Painel {
  protected readonly valorDe = valorDe;
  /** O que fazer hoje (atalhos) ou Acompanhamento (indicadores): vem do data da rota */
  readonly aba = input<'hoje' | 'sequencia' | 'numeros'>('hoje');
  private readonly apresentacao = inject(Apresentacao);
  protected readonly fluxo = rxResource({ params: () => (this.aba() === 'sequencia' ? true : undefined), stream: () => this.apresentacao.fluxo() });
  private readonly sessao = inject(Sessao);
  private readonly servico = inject(Indicadores);

  protected readonly usuario = this.sessao.usuario;
  protected readonly atalhos = computed(() => menuDoCargo(this.usuario()?.cargo).filter((i) => i.rota !== '/painel'));
  protected readonly indicadores = rxResource({ stream: () => this.servico.listar() });
  protected readonly erro = computed(() => (this.indicadores.error() ? mensagemDeErro(this.indicadores.error()) : null));
  protected readonly erroFluxo = computed(() => (this.fluxo.error() ? mensagemDeErro(this.fluxo.error()) : null));
  /** Os que pedem atenção primeiro: em destaque no "O que fazer hoje" e no alto do Acompanhamento. */
  protected readonly vermelhos = computed(() => (valorDe(this.indicadores) ?? []).filter((i) => i.vermelho));
  protected readonly noVermelho = computed(() => this.vermelhos().length);
  protected readonly ordenados = computed(() => [...(valorDe(this.indicadores) ?? [])].sort((a, b) => Number(b.vermelho) - Number(a.vermelho)));
  private readonly router = inject(Router);

  /** A tela para verificar o indicador, se o cargo pode abri-la (a mesma regra do data.cargos da rota). */
  protected verificar(i: IndicadorDto): Verificacao | null {
    const v = VERIFICAR[i.chave];
    const casca = this.router.config.find((r) => r.path === '' && r.children);
    const rota = v && casca?.children?.find((r) => r.path === v.rota.slice(1));
    const cargos = rota?.data?.['cargos'] as Cargo[] | undefined;
    return rota && (!cargos || this.sessao.temCargo(...cargos)) ? v : null;
  }

  protected valor(i: IndicadorDto): string {
    if (i.valor === null) return '—';
    return i.valor.toLocaleString('pt-BR', { maximumFractionDigits: 2 });
  }
}