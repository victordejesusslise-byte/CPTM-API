import { Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { data, m3, mensagemDeErro } from '../../helpers/formato';
import { PedidoDto } from '../../models/pedido';
import { Pedidos } from '../../services/pedidos';
import { Estado } from '../../shared/estado/estado';
import { Coluna, Tabela } from '../../shared/tabela/tabela';
import { FORMULARIO } from '../../shared/ui';

/** A lista de pedidos (supervisor e operador de pátio). */
@Component({
  selector: 'app-pedidos',
  imports: [RouterLink, Estado, Tabela, FORMULARIO],
  template: `
    <p class="sobre">Peça · 1</p>
    <h1>Pedidos</h1>
    <div class="acoes"><a mat-flat-button routerLink="/pedidos/novo">Novo pedido</a></div>
    <app-estado [carregando]="pedidos.isLoading()" [erro]="erro()" [vazio]="pedidos.hasValue() && !pedidos.value().length" textoVazio="Nenhum pedido ainda." />
    @if (pedidos.hasValue() && pedidos.value().length) {
      <app-tabela [colunas]="colunas" [linhas]="pedidos.value()" [clicavel]="true" (escolher)="abrir($event)" />
    }
  `,
})
export class ListaPedidos {
  private readonly servico = inject(Pedidos);
  private readonly router = inject(Router);
  protected readonly pedidos = rxResource({ stream: () => this.servico.listar() });
  protected readonly erro = computed(() => (this.pedidos.error() ? mensagemDeErro(this.pedidos.error()) : null));
  protected readonly colunas: Coluna<PedidoDto>[] = [
    { titulo: 'Código', valor: (p) => p.codigo, mono: true },
    { titulo: 'Fornecedor', valor: (p) => p.empresa },
    { titulo: 'Compra', valor: (p) => data(p.dataCompra) },
    { titulo: 'Peças', valor: (p) => `${p.pecasRecebidas} de ${p.quantidade}` },
    { titulo: 'Volume', valor: (p) => m3(p.volumeM3) },
    { titulo: 'Situação', valor: (p) => p.situacao, vermelho: (p) => p.situacao === 'Cancelado' },
  ];

  protected abrir(p: PedidoDto): void {
    this.router.navigate(['/pedidos', p.id]);
  }
}