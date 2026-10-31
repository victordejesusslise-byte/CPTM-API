import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, InjectionToken, computed, effect, inject, signal, untracked } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import type { HubConnection } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { AlertaDto, TrocaDto } from '../../campo/entidades/campo';
import { StatusTroca } from '../../compartilhado/entidades/enums';
import { PecaDto } from '../../pecas/entidades/peca';
import { PassagemDto } from '../../via/servicos/passagens';
import { TrechoDoMapaDto } from '../../via/servicos/trechos';
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

/** O pacote do operador da via (GET /api/campo/operador?dias=): os trechos, os alertas, as trocas e as passagens dos últimos dias. */
export interface PacoteDoOperadorDto {
  dias: number;
  geradoEm: string;
  trechos: TrechoDoMapaDto[];
  alertas: AlertaDto[];
  trocas: TrocaDto[];
  passagens: PassagemDto[];
}

export interface VersaoDoApp {
  versao: string;
  baixar: string;
}

const CHAVE = 'ybyra.plano';
const CHAVE_OPERADOR = 'ybyra.operador';
const CHAVE_DIAS = 'ybyra.dias';
const HUB = ambiente.api.replace(/\/api$/, '') + '/hub/campo';

/** "0.1.12" > "0.1.9": compara número a número. */
export const versaoMaisNova = (servidor: string, app: string): boolean => {
  const [a, b] = [servidor, app].map((v) => v.split('.').map((n) => Number(n) || 0));
  for (let i = 0; i < Math.max(a.length, b.length); i++) if ((a[i] ?? 0) !== (b[i] ?? 0)) return (a[i] ?? 0) > (b[i] ?? 0);
  return false;
};

/** O plano primeiro (é o mais novo da noite); do pacote do operador, só o que o plano não tem. */
const juntar = <T extends { id: number }>(plano: T[] = [], pacote: T[] = []): T[] => [...plano, ...pacote.filter((p) => !plano.some((q) => q.id === p.id))];

/**
 * O trabalho da noite no celular (app Android, Etapa 7, 2026-10-24): com rede, baixa o plano (as trocas da noite, os alertas abertos e as
 * peças deles), guarda no celular e fica ligado no ao vivo (SignalR): quando o supervisor aprova, o pátio separa ou o carro abre um
 * alerta, o plano é baixado de novo na hora. Sem rede, as telas de troca, inspeção e vistoria abrem pelo que ficou guardado.
 * Também confere se tem versão nova do app.
 * Para quem trabalha na via, guarda junto o pacote do operador (Victor, 2026-10-26): os trechos (o mapa sai da cópia das linhas), os alertas,
 * as trocas e as passagens dos últimos 7 ou 30 dias (a pessoa escolhe). Tocar em Sair não apaga o que está guardado.
 */
@Injectable({ providedIn: 'root' })
export class TrabalhoDaNoite {
  private readonly http = inject(HttpClient);
  private readonly sessao = inject(Sessao);
  private readonly rede = inject(Rede);
  private readonly guardado = inject(GUARDADO);
  private readonly noApp = inject(NO_APP);

  readonly plano = signal<PlanoCampoDto | null>(null);
  readonly pacote = signal<PacoteDoOperadorDto | null>(null);
  /** Quantos dias de alertas, trocas e passagens ficam no celular. */
  readonly dias = signal<7 | 30>(7);
  /** O pacote do operador é para quem trabalha na via. */
  readonly recebePacote = computed(() => this.sessao.temCargo('OperadorVia', 'Tecnico', 'Supervisor'));
  /** Quando o que está guardado veio do servidor (o mais velho entre o plano e o pacote): "atualizado em …" nas telas sem rede. */
  readonly atualizadoEm = computed(() => [this.plano()?.geradoEm, this.pacote()?.geradoEm].filter((d): d is string => !!d).sort()[0] ?? null);
  /** No app e sem rede: as telas de campo leem o plano guardado. */
  readonly usarGuardado = computed(() => this.noApp && !this.rede.online());
  /** Aviso de troca urgente que chegou ao vivo (fica até a pessoa fechar). */
  readonly urgente = signal<string | null>(null);
  readonly versaoNova = signal<VersaoDoApp | null>(null);
  private conexao?: HubConnection;
  private versaoConferida = false;
  /** O que estava guardado já foi lido (a escolha dos dias vem antes de baixar o pacote). */
  private readonly pronto: Promise<void>;

  constructor() {
    this.pronto = this.noApp ? this.carregar() : Promise.resolve();
    if (!this.noApp) return;
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

  /** As trocas do plano e, do pacote, as feitas e as recusadas dos últimos dias. */
  private todasAsTrocas(): TrocaDto[] {
    return juntar(this.plano()?.trocas, this.pacote()?.trocas);
  }
  private todosOsAlertas(): AlertaDto[] {
    return juntar(this.plano()?.alertas, this.pacote()?.alertas);
  }
  trocas(status: StatusTroca[]): TrocaDto[] {
    return this.todasAsTrocas().filter((t) => status.includes(t.status));
  }
  troca(id: number): TrocaDto | null {
    return this.todasAsTrocas().find((t) => t.id === id) ?? null;
  }
  alertas(origem: AlertaDto['origem'], status: AlertaDto['status'] | null = null): AlertaDto[] {
    return this.todosOsAlertas().filter((a) => a.origem === origem && (!status || a.status === status));
  }
  alerta(id: number): AlertaDto | null {
    return this.todosOsAlertas().find((a) => a.id === id) ?? null;
  }
  pecaPorTag(tag: string): PecaDto | null {
    return this.plano()?.pecas.find((p) => p.tag === tag.trim()) ?? null;
  }

  /** Baixa o plano (e o pacote do operador) e guarda no celular. Sem rede (ou servidor fora), fica o que já estava guardado. */
  async atualizar(): Promise<void> {
    await Promise.all([this.baixarPlano(), this.baixarPacote()]);
  }

  /** A pessoa escolhe quantos dias guardar (7 ou 30): vale para os alertas, as trocas e as passagens. */
  async escolherDias(dias: 7 | 30): Promise<void> {
    this.dias.set(dias);
    await this.guardado.gravar(CHAVE_DIAS, String(dias)).catch(() => undefined);
    if (this.rede.online()) await this.baixarPacote();
  }

  private async baixarPacote(): Promise<void> {
    await this.pronto;
    if (!this.recebePacote()) return;
    try {
      const pacote = await firstValueFrom(this.http.get<PacoteDoOperadorDto>(`${ambiente.api}/campo/operador`, { params: { dias: this.dias() } }));
      this.pacote.set(pacote);
      await this.guardado.gravar(CHAVE_OPERADOR, JSON.stringify(pacote));
    } catch {
      /* fica o guardado */
    }
  }

  private async baixarPlano(): Promise<void> {
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
      const pacote = await this.guardado.ler(CHAVE_OPERADOR);
      if (pacote && !this.pacote()) this.pacote.set(JSON.parse(pacote) as PacoteDoOperadorDto);
      if ((await this.guardado.ler(CHAVE_DIAS)) === '30') this.dias.set(30);
    } catch {
      /* nada guardado */
    }
  }

  private async conectar(): Promise<void> {
    if (this.conexao) return;
    const { HubConnectionBuilder } = await import('@microsoft/signalr');
    const conexao = new HubConnectionBuilder()
      .withUrl(HUB, { accessTokenFactory: () => this.sessao.token() ?? '', withCredentials: false })
      // tenta voltar por uns 2 minutos: aguenta o servidor reiniciando (por exemplo, para anunciar uma versão nova do app)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000, 30000, 60000])
      .build();
    for (const evento of ['trocaNoPlano', 'alertaNovo', 'alertaFechado']) conexao.on(evento, () => void this.atualizar());
    conexao.on('trocaUrgente', () => {
      this.urgente.set('Chegou uma troca urgente para esta noite. Confira em "A trocar".');
      void this.atualizar();
    });
    conexao.onreconnected(() => void this.aoReconectar());
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

  /**
   * O servidor voltou: baixa o plano de novo e confere a versão de novo. Reiniciar o servidor com uma versão nova anunciada
   * faz o aviso chegar a quem está com o app aberto, na hora (Victor, 2026-10-31); só aparece para quem tem uma versão mais velha.
   */
  async aoReconectar(): Promise<void> {
    this.versaoConferida = false;
    await Promise.all([this.atualizar(), this.conferirVersao()]);
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
