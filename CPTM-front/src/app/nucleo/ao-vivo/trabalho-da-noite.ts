import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, InjectionToken, computed, effect, inject, signal, untracked } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import type { HubConnection } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { AlertaDto, LoteTrocaDto, StatusLoteTroca, TrocaDto } from '../../campo/entidades/campo';
import { StatusTroca } from '../../compartilhado/entidades/enums';
import { PecaDto } from '../../pecas/entidades/peca';
import { PassagemDto } from '../../via/servicos/passagens';
import { TrechoDoMapaDto } from '../../via/servicos/trechos';
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

/** O pacote do operador da via (GET /api/campo/operador?dias=): os trechos, os alertas, as trocas e as passagens dos últimos dias. */
export interface PacoteDoOperadorDto {
  /** Quantos dias para trás o pacote traz (7 ou 30). */
  dias: number;
  /** Quando o servidor montou o pacote. */
  geradoEm: string;
  /** Os trechos, com as peças de cada um (o mapa do trecho sem rede). */
  trechos: TrechoDoMapaDto[];
  /** Os alertas dos últimos dias. */
  alertas: AlertaDto[];
  /** As trocas dos últimos dias. */
  trocas: TrocaDto[];
  /** As passagens dos últimos dias. */
  passagens: PassagemDto[];
}

/** A versão do app que o servidor anuncia (GET /api/app/versao). */
export interface VersaoDoApp {
  /** O número da versão (ex.: 0.1.40). */
  versao: string;
  /** O link para baixar o APK. */
  baixar: string;
}

/** Os nomes com que o plano, o pacote e a escolha dos dias ficam guardados no celular. */
const CHAVE = 'ybyra.plano';
const CHAVE_OPERADOR = 'ybyra.operador';
const CHAVE_DIAS = 'ybyra.dias';
/** O endereço do ao vivo (SignalR) no servidor. */
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
 * O trabalho da noite no app: com rede, baixa o plano da noite (e, para quem trabalha na via, o pacote dos últimos 7 ou 30 dias) e guarda no celular.
 * Fica ligado no ao vivo: quando algo muda no servidor (troca aprovada, alerta novo), baixa de novo. Sem rede, as telas usam o que ficou guardado.
 * Também avisa quando há versão nova do app. Sair não apaga o que está guardado.
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
  /** O pacote do operador guardado (ou nada ainda). */
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
  /** A versão nova do app, quando o servidor tem uma mais nova que esta. */
  readonly versaoNova = signal<VersaoDoApp | null>(null);
  /** A ligação com o ao vivo do servidor. */
  private conexao?: HubConnection;
  /** A versão já foi conferida nesta abertura? */
  private versaoConferida = false;
  /** O que estava guardado já foi lido (a escolha dos dias vem antes de baixar o pacote). */
  private readonly pronto: Promise<void>;

  constructor() {
    // No site não faz nada: as telas buscam direto no servidor.
    this.pronto = this.noApp ? this.carregar() : Promise.resolve();
    if (!this.noApp) return;
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

  /** As trocas do plano e, do pacote, as feitas e as recusadas dos últimos dias. */
  private todasAsTrocas(): TrocaDto[] {
    return juntar(this.plano()?.trocas, this.pacote()?.trocas);
  }
  /** Os alertas do plano e os do pacote, sem repetir. */
  private todosOsAlertas(): AlertaDto[] {
    return juntar(this.plano()?.alertas, this.pacote()?.alertas);
  }
  /** As trocas guardadas com estes status. */
  trocas(status: StatusTroca[]): TrocaDto[] {
    return this.todasAsTrocas().filter((t) => status.includes(t.status));
  }
  /** Uma troca guardada, pelo id. */
  troca(id: number): TrocaDto | null {
    return this.todasAsTrocas().find((t) => t.id === id) ?? null;
  }
  /** Os lotes de troca do plano com estes status. */
  lotes(status: StatusLoteTroca[]): LoteTrocaDto[] {
    return (this.plano()?.lotes ?? []).filter((l) => status.includes(l.status));
  }
  /** Um lote de troca do plano, pelo id. */
  lote(id: number): LoteTrocaDto | null {
    return this.plano()?.lotes?.find((l) => l.id === id) ?? null;
  }
  /** Os alertas guardados de uma origem (imagem ou sensor), e, se pedir, de um status. */
  alertas(origem: AlertaDto['origem'], status: AlertaDto['status'] | null = null): AlertaDto[] {
    return this.todosOsAlertas().filter((a) => a.origem === origem && (!status || a.status === status));
  }
  /** Um alerta guardado, pelo id. */
  alerta(id: number): AlertaDto | null {
    return this.todosOsAlertas().find((a) => a.id === id) ?? null;
  }
  /** A peça do plano com esta tag (para ler a tag sem rede). */
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

  /** Baixa o pacote do operador (GET /api/campo/operador?dias=) e guarda. Só para a via, o técnico e o supervisor. */
  private async baixarPacote(): Promise<void> {
    // Espera ler a escolha dos dias guardada.
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

  /** Baixa o plano da noite (GET /api/campo/plano) e guarda. */
  private async baixarPlano(): Promise<void> {
    try {
      const plano = await firstValueFrom(this.http.get<PlanoCampoDto>(`${ambiente.api}/campo/plano`));
      this.plano.set(plano);
      await this.guardado.gravar(CHAVE, JSON.stringify(plano));
    } catch {
      /* fica o guardado */
    }
  }

  /** Lê do celular o plano, o pacote e a escolha dos dias (ao abrir o app). Não troca o que já chegou do servidor. */
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

  /** Liga o ao vivo (SignalR), com o token de quem está logado. */
  private async conectar(): Promise<void> {
    if (this.conexao) return;
    // A biblioteca do SignalR só é carregada no app, quando precisa.
    const { HubConnectionBuilder } = await import('@microsoft/signalr');
    const conexao = new HubConnectionBuilder()
      .withUrl(HUB, { accessTokenFactory: () => this.sessao.token() ?? '', withCredentials: false })
      // tenta voltar por uns 2 minutos: aguenta o servidor reiniciando (por exemplo, para anunciar uma versão nova do app)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000, 30000, 60000])
      .build();
    // Qualquer mudança no plano ou nos alertas: baixa de novo.
    for (const evento of ['trocaNoPlano', 'loteNoPlano', 'alertaNovo', 'alertaFechado']) conexao.on(evento, () => void this.atualizar());
    // Troca urgente: mostra o aviso e baixa de novo.
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

  /** Desliga o ao vivo. */
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
