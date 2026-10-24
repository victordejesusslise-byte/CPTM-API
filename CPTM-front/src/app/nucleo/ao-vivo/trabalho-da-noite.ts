import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, InjectionToken, computed, effect, inject, signal, untracked } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import type { HubConnection } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { AlertaDto, TrocaDto } from '../../campo/entidades/campo';
import { StatusTroca } from '../../compartilhado/entidades/enums';
import { PecaDto } from '../../pecas/entidades/peca';
import { GUARDADO } from '../fila/guardado';
import { Rede } from '../fila/rede';
import { Sessao } from '../sessao/sessao';

/** Rodando no app Android (no site do navegador, as telas continuam buscando no servidor, como sempre). */
export const NO_APP = new InjectionToken<boolean>('Rodando no app Android', { providedIn: 'root', factory: () => Capacitor.isNativePlatform() });

export interface PlanoCampoDto {
  noite: string;
  geradoEm: string;
  trocas: TrocaDto[];
  alertas: AlertaDto[];
  pecas: PecaDto[];
}

export interface VersaoDoApp {
  versao: string;
  baixar: string;
}

const CHAVE = 'ybyra.plano';
const HUB = ambiente.api.replace(/\/api$/, '') + '/hub/campo';

/** "0.1.12" > "0.1.9": compara número a número. */
export const versaoMaisNova = (servidor: string, app: string): boolean => {
  const [a, b] = [servidor, app].map((v) => v.split('.').map((n) => Number(n) || 0));
  for (let i = 0; i < Math.max(a.length, b.length); i++) if ((a[i] ?? 0) !== (b[i] ?? 0)) return (a[i] ?? 0) > (b[i] ?? 0);
  return false;
};

/**
 * O trabalho da noite no celular (app Android, Etapa 7, 2026-10-24): com rede, baixa o plano (as trocas da noite, os alertas abertos e as
 * peças deles), guarda no celular e fica ligado no ao vivo (SignalR): quando o supervisor aprova, o pátio separa ou o carro abre um
 * alerta, o plano é baixado de novo na hora. Sem rede, as telas de troca, inspeção e vistoria abrem pelo que ficou guardado.
 * Também confere se tem versão nova do app.
 */
@Injectable({ providedIn: 'root' })
export class TrabalhoDaNoite {
  private readonly http = inject(HttpClient);
  private readonly sessao = inject(Sessao);
  private readonly rede = inject(Rede);
  private readonly guardado = inject(GUARDADO);
  private readonly noApp = inject(NO_APP);

  readonly plano = signal<PlanoCampoDto | null>(null);
  /** No app e sem rede: as telas de campo leem o plano guardado. */
  readonly usarGuardado = computed(() => this.noApp && !this.rede.online());
  /** Aviso de troca urgente que chegou ao vivo (fica até a pessoa fechar). */
  readonly urgente = signal<string | null>(null);
  readonly versaoNova = signal<VersaoDoApp | null>(null);
  private conexao?: HubConnection;
  private versaoConferida = false;

  constructor() {
    if (!this.noApp) return;
    void this.carregar();
    inject(DestroyRef).onDestroy(() => void this.desconectar());
    effect(() => {
      const quem = this.sessao.usuario();
      const online = this.rede.online();
      untracked(() => {
        if (quem && online) {
          void this.atualizar();
          void this.conectar();
          void this.conferirVersao();
        } else if (!quem) void this.desconectar();
      });
    });
  }

  trocas(status: StatusTroca[]): TrocaDto[] {
    return (this.plano()?.trocas ?? []).filter((t) => status.includes(t.status));
  }
  troca(id: number): TrocaDto | null {
    return this.plano()?.trocas.find((t) => t.id === id) ?? null;
  }
  alertas(origem: AlertaDto['origem']): AlertaDto[] {
    return (this.plano()?.alertas ?? []).filter((a) => a.origem === origem);
  }
  alerta(id: number): AlertaDto | null {
    return this.plano()?.alertas.find((a) => a.id === id) ?? null;
  }
  pecaPorTag(tag: string): PecaDto | null {
    return this.plano()?.pecas.find((p) => p.tag === tag.trim()) ?? null;
  }

  /** Baixa o plano e guarda no celular. Sem rede (ou servidor fora), fica o que já estava guardado. */
  async atualizar(): Promise<void> {
    try {
      const plano = await firstValueFrom(this.http.get<PlanoCampoDto>(`${ambiente.api}/campo/plano`));
      this.plano.set(plano);
      await this.guardado.gravar(CHAVE, JSON.stringify(plano));
    } catch {
      /* fica o guardado */
    }
  }

  private async carregar(): Promise<void> {
    try {
      const salvo = await this.guardado.ler(CHAVE);
      if (salvo && !this.plano()) this.plano.set(JSON.parse(salvo) as PlanoCampoDto);
    } catch {
      /* nada guardado */
    }
  }

  private async conectar(): Promise<void> {
    if (this.conexao) return;
    const { HubConnectionBuilder } = await import('@microsoft/signalr');
    const conexao = new HubConnectionBuilder()
      .withUrl(HUB, { accessTokenFactory: () => this.sessao.token() ?? '', withCredentials: false })
      .withAutomaticReconnect()
      .build();
    for (const evento of ['trocaNoPlano', 'alertaNovo', 'alertaFechado']) conexao.on(evento, () => void this.atualizar());
    conexao.on('trocaUrgente', () => {
      this.urgente.set('Chegou uma troca urgente para esta noite. Confira em "A trocar".');
      void this.atualizar();
    });
    conexao.onreconnected(() => void this.atualizar());
    this.conexao = conexao;
    try {
      await conexao.start();
    } catch {
      this.conexao = undefined; // tenta de novo na próxima vez que a rede voltar
    }
  }

  private async desconectar(): Promise<void> {
    const conexao = this.conexao;
    this.conexao = undefined;
    await conexao?.stop().catch(() => undefined);
  }

  private async conferirVersao(): Promise<void> {
    if (this.versaoConferida || !ambiente.versao) return;
    try {
      const servidor = await firstValueFrom(this.http.get<VersaoDoApp>(`${ambiente.api}/app/versao`));
      this.versaoConferida = true;
      if (servidor.versao && versaoMaisNova(servidor.versao, ambiente.versao)) this.versaoNova.set(servidor);
    } catch {
      /* confere na próxima vez */
    }
  }
}
