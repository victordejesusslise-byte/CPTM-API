import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Pedidos } from '../../../pedidos/servicos/pedidos';
import { FORMULARIO } from '../../../compartilhado/ui';
import { LerPlaca } from './ler-placa';

/**
 * Chegada → Placa do feixe: a foto da placa lida pelo OCR no servidor; o operador confere e corrige cada campo.
 * A mesma leitura está dentro de cada feixe da Chegada da carga, que preenche o feixe com a placa.
 */
@Component({
  selector: 'app-placa-feixe',
  imports: [FORMULARIO, LerPlaca],
  templateUrl: './placa-feixe.html',
})
export class PlacaFeixe {
  private readonly pedidos = inject(Pedidos);
  protected readonly lista = rxResource({ stream: () => this.pedidos.listar() });
  protected readonly abertos = computed(() => (this.lista.value() ?? []).filter((p) => p.situacao !== 'Cancelado' && p.situacao !== 'Recebido'));
  protected readonly idPedido = signal<number | null>(null);
}
