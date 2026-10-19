import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Pedidos } from '../../../pedidos/servicos/pedidos';
import { FORMULARIO } from '../../../compartilhado/ui';
import { LerPlaca } from './ler-placa';

/**
 * Chegada → Placa do feixe: a foto da placa lida pelo OCR no servidor; o operador confere e corrige cada campo.
 * A placa é sempre de um pedido (Victor, 2026-10-17): confirmada, ela já aparece no feixe em Registrar chegada daquele pedido.
 */
@Component({
  selector: 'app-placa-feixe',
  imports: [Esqueleto, RouterLink, FORMULARIO, LerPlaca],
  templateUrl: './placa-feixe.html',
})
export class PlacaFeixe {
  /** ?pedido= vindo da chegada */
  readonly pedido = input<string>();
  private readonly pedidos = inject(Pedidos);
  protected readonly lista = rxResource({ stream: () => this.pedidos.listar() });
  protected readonly abertos = computed(() => (this.lista.value() ?? []).filter((p) => p.situacao !== 'Cancelado' && p.situacao !== 'Recebido'));
  protected readonly idPedido = linkedSignal(() => (this.pedido() ? Number(this.pedido()) : null));
  /** Quantas placas foram confirmadas nesta tela para o pedido escolhido. */
  protected readonly confirmadas = linkedSignal({ source: this.idPedido, computation: () => 0 });
}
