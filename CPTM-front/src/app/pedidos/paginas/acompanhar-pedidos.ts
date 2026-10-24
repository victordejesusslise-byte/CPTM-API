import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { data, mensagemDeErro } from '../../compartilhado/utilitarios/formato';
import { PedidoDto } from '../entidades/pedido';
import { Pedidos } from '../servicos/pedidos';
import { Estado } from '../../compartilhado/componentes/estado/estado';
import { FORMULARIO } from '../../compartilhado/ui';
import { PedidoAberto } from './pedido-aberto';
import { OPCOES_SITUACAO, PERIODOS, Periodo, dentroDoPeriodo, entregaIncompleta, faltaChegar, situacao } from './situacao';
import { CartoesNoCelular } from '../../compartilhado/componentes/tabela/cartoes';

/**
 * Acompanhar pedidos (protótipo YBYRA, Pedidos → Acompanhar pedidos): totais do filtro, período, busca e situação,
 * a lista e, em cima, o pedido aberto para revisar, corrigir ou cancelar.
 */
@Component({
  selector: 'app-acompanhar-pedidos',
  imports: [CartoesNoCelular, FormsModule, FORMULARIO, Estado, PedidoAberto],
  templateUrl: './acompanhar-pedidos.html',
})
export class AcompanharPedidos {
  /** ?aberto= e ?registrado= vindos da URL */
  readonly aberto = input<string>();
  readonly registrado = input<string>();
  private readonly servico = inject(Pedidos);
  private readonly router = inject(Router);

  protected readonly pedidos = rxResource({ stream: () => this.servico.listar() });
  protected readonly erro = computed(() => (this.pedidos.error() ? mensagemDeErro(this.pedidos.error()) : null));
  protected readonly idAberto = computed(() => (this.aberto() ? Number(this.aberto()) : null));

  protected readonly periodos = PERIODOS;
  protected readonly situacoes = OPCOES_SITUACAO;
  protected readonly periodo = signal<Periodo>('mes');
  protected readonly busca = signal('');
  protected readonly filtroSituacao = signal('all');

  private readonly todos = computed(() => this.pedidos.value() ?? []);
  protected readonly contagem = computed(() => {
    const c = {} as Record<Periodo, number>;
    for (const p of PERIODOS) c[p.valor] = this.todos().filter((x) => dentroDoPeriodo(x.dataCompra, p.valor)).length;
    return c;
  });
  protected readonly lista = computed(() => {
    const q = this.busca().trim().toLowerCase();
    const s = this.filtroSituacao();
    return this.todos()
      .filter((p) => dentroDoPeriodo(p.dataCompra, this.periodo()))
      .filter((p) => (s === 'all' ? true : s === '__falta' ? faltaChegar(p) > 0 : p.situacao === s))
      .filter((p) => !q || p.codigo.toLowerCase().includes(q) || p.empresa.toLowerCase().includes(q));
  });
  protected readonly totais = computed(() => {
    const l = this.lista();
    return {
      pediu: l.reduce((a, p) => a + p.quantidade, 0),
      chegou: l.reduce((a, p) => a + p.pecasRecebidas, 0),
      falta: l.reduce((a, p) => a + faltaChegar(p), 0),
      incompletas: l.filter(entregaIncompleta).length,
    };
  });

  protected readonly data = data;
  protected readonly situacao = situacao;
  protected readonly faltaChegar = faltaChegar;
  protected readonly entregaIncompleta = entregaIncompleta;
  protected readonly porcento = (p: PedidoDto) => (p.quantidade ? Math.min(100, Math.round((p.pecasRecebidas / p.quantidade) * 100)) : 0);

  protected abrir(p: PedidoDto): void {
    this.router.navigate(['/pedidos'], { queryParams: { aberto: p.id } });
  }

  protected fechar(): void {
    this.router.navigate(['/pedidos']);
  }
}