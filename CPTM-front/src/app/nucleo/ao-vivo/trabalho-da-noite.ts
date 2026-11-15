import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, InjectionToken, computed, effect, inject, signal, untracked } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import type { HubConnection } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { AlertaDto, LoteTrocaDto, StatusLoteTroca, TrocaDto } from '../../campo/entidades/campo';
import { StatusTroca } from '../../compartilhado/entidades/enums';
import { PecaDto } from '../../pecas/entidades/peca';
import { GUARDADO } from '../fila/guardado';
import { Rede } from '../fila/rede';
import { Sessao } from '../sessao/sessao';

/** Rodando no app Android (no site do navegador, as telas continuam buscando no servidor, como sempre). */
export const NO_APP = new InjectionToken<boolean>('Rodando no app Android', { providedIn: 'root', factory: () => Capacitor.isNativePlatform() });

/** O plano da noite (GET /api/campo/plano): o que a via vai fazer nesta noite. */
export interface PlanoCampoDto {
  /** A noite do plano (data). */
  noite: string;
  /** Quando o servidor montou o plano. */
  geradoEm: string;
  /** As trocas da noite (aprovadas e separadas). */
  trocas: TrocaDto[];
  /** Os alertas abertos. */
  alertas: AlertaDto[];
  /** As peças das trocas e dos alertas (para achar pela tag sem rede). */
  pecas: PecaDto[];
  /** Os lotes de troca da noite, com a faixa e as separadas (a via confere sem rede). */
  lotes?: LoteTrocaDto[] | null;
}

/** A versão do app que o servidor anuncia (GET /api/app/versao). */
export interface VersaoDoApp {
  /** O número da versão (ex.: 0.1.40). */
  versao: string;
  /** O link para baixar o APK. */
  baixar: string;
}

/** O nome com que o plano fica guardado no celular. */
const CHAVE = 'ybyra.plano';
/** O endereço do ao vivo (SignalR) no servidor. */
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

  /** O plano da noite guardado (ou nada ainda). */
  readonly plano = signal<PlanoCampoDto | null>(null);
  /** No app e sem rede: as telas de campo leem o plano guardado. */
  readonly usarGuardado = computed(() => this.noApp && !this.rede.online());
  /** Aviso de troca urgente que chegou ao vivo (fica até a pessoa fechar). */
  readonly urgente = signal<string | null>(null);
  /** A versão nova do app, quando o servidor tem uma mais nova que esta. */
  readonly versaoNova = signal<VersaoDoApp | null>(null);
  /** A ligação com o ao vivo do servidor. */
  private conexao?: HubConnection;
  /** A versão já foi conferida nesta abertura? */
  private versaoConferida = false;

  constructor() {
    // No site não faz nada: as telas buscam direto no servidor.
    if (!this.noApp) return;
    void this.carregar();
    inject(DestroyRef).onDestroy(() => void this.desconectar());
    // Com alguém logado e com rede: baixa, liga o ao vivo e confere a versão. Sem ninguém: desliga o ao vivo.
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

  /** As trocas guardadas com estes status. */
  trocas(status: StatusTroca[]): TrocaDto[] {
    return (this.plano()?.trocas ?? []).filter((t) => status.includes(t.status));
  }
  /** Uma troca guardada, pelo id. */
  troca(id: number): TrocaDto | null {
    return this.plano()?.trocas.find((t) => t.id === id) ?? null;
  }
  /** Os lotes de troca do plano com estes status. */
  lotes(status: StatusLoteTroca[]): LoteTrocaDto[] {
    return (this.plano()?.lotes ?? []).filter((l) => status.includes(l.status));
  }
  /** Um lote de troca do plano, pelo id. */
  lote(id: number): LoteTrocaDto | null {
    return this.plano()?.lotes?.find((l) => l.id === id) ?? null;
  }
  /** Os alertas guardados de uma origem (imagem ou sensor). */
  alertas(origem: AlertaDto['origem']): AlertaDto[] {
    return (this.plano()?.alertas ?? []).filter((a) => a.origem === origem);
  }
  /** Um alerta guardado, pelo id. */
  alerta(id: number): AlertaDto | null {
    return this.plano()?.alertas.find((a) => a.id === id) ?? null;
  }
  /** A peça do plano com esta tag (para ler a tag sem rede). */
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

  /** Lê do celular o plano guardado (ao abrir o app). Não troca o que já chegou do servidor. */
  private async carregar(): Promise<void> {
    try {
      const salvo = await this.guardado.ler(CHAVE);
      if (salvo && !this.plano()) this.plano.set(JSON.parse(salvo) as PlanoCampoDto);
    } catch {
      /* nada guardado */
    }
  }

  /** Liga o ao vivo (SignalR), com o token de quem está logado. */
  private async conectar(): Promise<void> {
    if (this.conexao) return;
    // A biblioteca do SignalR só é carregada no app, quando precisa.
    const { HubConnectionBuilder } = await import('@microsoft/signalr');
    const conexao = new HubConnectionBuilder()
      .withUrl(HUB, { accessTokenFactory: () => this.sessao.token() ?? '', withCredentials: false })
      .withAutomaticReconnect()
      .build();
    // Qualquer mudança no plano ou nos alertas: baixa de novo.
    for (const evento of ['trocaNoPlano', 'loteNoPlano', 'alertaNovo', 'alertaFechado']) conexao.on(evento, () => void this.atualizar());
    // Troca urgente: mostra o aviso e baixa de novo.
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

  /** Desliga o ao vivo. */
  private async desconectar(): Promise<void> {
    const conexao = this.conexao;
    this.conexao = undefined;
    await conexao?.stop().catch(() => undefined);
  }

  /** Pergunta ao servidor a versão do app (GET /api/app/versao); se for mais nova que esta, mostra o aviso. */
  private async conferirVersao(): Promise<void> {
    // Uma vez por abertura, e só no app (o site não tem versão).
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
