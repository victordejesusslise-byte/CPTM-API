import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import type { HubConnection } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { TrocaDto } from '../../campo/entidades/campo';
import { Sessao } from '../sessao/sessao';

const HUB = ambiente.api.replace(/\/api$/, '') + '/hub/campo';

/**
 * Os pedidos de troca esperando aprovação, para quem aprova (supervisor e técnico; Victor, 2026-10-31: "a aprovação tá meio confusa"):
 * o número ao lado de "Aprovar trocas" no menu e o aviso quando chega um pedido novo, ao vivo (trocaPedida), no site e no app.
 */
@Injectable({ providedIn: 'root' })
export class PedidosDeTroca {
  private readonly http = inject(HttpClient);
  private readonly sessao = inject(Sessao);

  readonly aprova = computed(() => this.sessao.temCargo('Supervisor', 'Tecnico'));
  /** Quantos pedidos esperam a aprovação. */
  readonly quantas = signal(0);
  /** O pedido que acabou de chegar ao vivo (o aviso fica até a pessoa fechar). */
  readonly chegou = signal<number | null>(null);
  private conexao?: HubConnection;

  constructor() {
    inject(DestroyRef).onDestroy(() => void this.desconectar());
    effect(() => {
      const quem = this.sessao.usuario()?.id;
      const aprova = this.aprova();
      untracked(() => {
        if (quem && aprova) {
          void this.contar();
          void this.conectar();
        } else {
          this.quantas.set(0);
          this.chegou.set(null);
          void this.desconectar();
        }
      });
    });
  }

  /** Conta de novo (ao entrar, ao vivo e depois de pedir ou decidir). Sem rede, fica o número que estava. */
  async contar(): Promise<void> {
    if (!this.aprova()) return;
    try {
      const pedidas = await firstValueFrom(this.http.get<TrocaDto[]>(`${ambiente.api}/trocas`, { params: { status: 'Pedida' } }));
      this.quantas.set(pedidas.length);
    } catch {
      /* fica o número que estava */
    }
  }

  private async conectar(): Promise<void> {
    if (this.conexao) return;
    const { HubConnectionBuilder } = await import('@microsoft/signalr');
    const conexao = new HubConnectionBuilder()
      .withUrl(HUB, { accessTokenFactory: () => this.sessao.token() ?? '', withCredentials: false })
      .withAutomaticReconnect()
      .build();
    conexao.on('trocaPedida', (aviso: { id: number }) => {
      this.chegou.set(aviso?.id ?? null);
      void this.contar();
    });
    // aprovada, reprovada ou cancelada em outro lugar: o número muda
    for (const evento of ['trocaNoPlano', 'trocaUrgente']) conexao.on(evento, () => void this.contar());
    conexao.onreconnected(() => void this.contar());
    this.conexao = conexao;
    try {
      await conexao.start();
    } catch {
      this.conexao = undefined; // tenta de novo na próxima entrada
    }
  }

  private async desconectar(): Promise<void> {
    const conexao = this.conexao;
    this.conexao = undefined;
    await conexao?.stop().catch(() => undefined);
  }
}
