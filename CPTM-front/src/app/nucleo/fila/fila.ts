import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { novoIdCliente } from '../../compartilhado/utilitarios/arquivo';
import { mensagemDeErro } from '../../compartilhado/utilitarios/formato';
import { Sessao } from '../sessao/sessao';
import { GUARDADO, reduzirFoto } from './guardado';
import { Rede } from './rede';

/** O que vai pela fila: a troca feita, a recusa, a inspeção, a vistoria e o lote de troca fechado ou recusado (tudo o que se faz no campo sem rede). */
export type TipoRegistro = 'troca' | 'recusa' | 'inspecao' | 'vistoria' | 'lote' | 'recusa-lote';

export interface NovoRegistro {
  tipo: TipoRegistro;
  /** A troca (troca e recusa), a peça (inspeção), o alerta (vistoria) ou o lote de troca (lote e recusa-lote). */
  alvo: number;
  /** O registro como a API pede, sem a foto, o ID do celular e a hora (a fila põe). */
  corpo: Record<string, unknown>;
  /** Para a lista: "Troca da peça DM-…". */
  rotulo: string;
  /** Um retrato do que o registro mexe (a troca), para a tela mostrar na hora, sem rede, como já feito. */
  resumo?: unknown;
}

export interface ItemFila extends NovoRegistro {
  /** O ID do celular (idCliente): o mesmo registro mandado de novo entra uma vez no servidor. */
  id: string;
  idUsuario: number;
  /** A hora do campo (registradoEm). */
  criadoEm: string;
  foto: string;
  /** O caminho da foto no servidor, depois que ela subiu (no reenvio não sobe de novo). */
  arquivo?: string;
  /** O lote de troca: uma foto por dormente (a primeira é também `foto`), cada uma vai no par da mesma posição. */
  fotos?: string[];
  /** Os caminhos das fotos do lote no servidor, na ordem dos pares, conforme sobem. */
  arquivos?: string[];
  tentativas: number;
  /** Por que o servidor recusou (Não enviados). */
  motivo?: string;
}

export type ResultadoRegistro = { estado: 'enviado'; resposta: unknown } | { estado: 'guardado' } | { estado: 'recusado'; motivo: string };

const CHAVE = 'ybyra.fila';
const DESTINO: Record<TipoRegistro, { foto: (alvo: number) => string; registro: (alvo: number) => string }> = {
  troca: { foto: (id) => `/trocas/${id}/foto`, registro: (id) => `/trocas/${id}/execucao` },
  recusa: { foto: (id) => `/trocas/${id}/foto`, registro: (id) => `/trocas/${id}/recusa` },
  inspecao: { foto: (id) => `/dormentes/${id}/fotos/inspecao`, registro: () => '/inspecoes' },
  vistoria: { foto: (id) => `/alertas/${id}/foto`, registro: (id) => `/alertas/${id}/vistoria` },
  lote: { foto: (id) => `/lotes-troca/${id}/foto`, registro: (id) => `/lotes-troca/${id}/fechamento` },
  'recusa-lote': { foto: (id) => `/lotes-troca/${id}/foto`, registro: (id) => `/lotes-troca/${id}/recusa` },
};

/**
 * O corpo que vai para a API: no lote, cada foto vai no par da mesma posição, e as fotos depois das dos pares vão nos "não deu" das
 * obrigatórias, na ordem; nos outros, a foto vai em fotoArquivo.
 */
const corpoComFotos = (item: ItemFila, arquivos: string[]): Record<string, unknown> => {
  if (!item.fotos) return { ...item.corpo, fotoArquivo: arquivos[0] };
  const pares = (item.corpo['pares'] as Record<string, unknown>[] | undefined) ?? [];
  const naoDeu = (item.corpo['naoDeu'] as Record<string, unknown>[] | undefined) ?? [];
  return {
    ...item.corpo,
    pares: pares.map((par, i) => ({ ...par, fotoArquivo: arquivos[i] })),
    ...(naoDeu.length ? { naoDeu: naoDeu.map((n, j) => ({ ...n, fotoArquivo: arquivos[pares.length + j] })) } : {}),
  };
};

/**
 * A fila sem internet (app Android, Etapa 6, 2026-10-24): a troca, a recusa, a inspeção e a vistoria ficam guardadas no celular
 * (com a foto) e sobem sozinhas, na ordem, quando a rede volta: primeiro a foto, depois o registro com o caminho dela, o ID do
 * celular e a hora do campo. O que o servidor recusa vai para "Não enviados" (só no celular). Só sobem os registros de quem está
 * logado; com o login vencido, o servidor responde 401, o sistema pede a senha e, depois de entrar, a fila continua.
 */
@Injectable({ providedIn: 'root' })
export class Fila {
  private readonly http = inject(HttpClient);
  private readonly sessao = inject(Sessao);
  private readonly rede = inject(Rede);
  private readonly guardado = inject(GUARDADO);

  readonly itens = signal<ItemFila[]>([]);
  readonly naoEnviados = signal<ItemFila[]>([]);
  readonly enviando = signal(false);
  readonly online = this.rede.online;
  /** Os que esperam e são de quem está logado. O registro é de quem fez: só ele envia e só ele vê (Victor, 2026-10-25). */
  readonly meus = computed(() => this.itens().filter((i) => i.idUsuario === this.sessao.usuario()?.id));
  /** Os recusados de quem está logado (os de outro usuário do mesmo celular não aparecem para ele). */
  readonly meusNaoEnviados = computed(() => this.naoEnviados().filter((i) => i.idUsuario === this.sessao.usuario()?.id));
  private readonly carregada = this.carregar();
  private readonly respostas = new Map<string, unknown>();
  private espera?: ReturnType<typeof setTimeout>;

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.espera));
    // A rede voltou, ou alguém entrou: manda o que está esperando.
    effect(() => {
      if (this.rede.online() && this.sessao.usuario()) untracked(() => void this.enviarTudo());
    });
  }

  /**
   * Guarda o registro com a foto e já tenta mandar. Enviado: o servidor recebeu (com a resposta dele). Guardado: sem rede, sobe depois.
   * Recusado: o servidor não aceitou; a tela mostra o motivo e o registro sai da fila (quem está na frente corrige e manda de novo).
   */
  async registrar(novo: NovoRegistro, foto: Blob | Blob[]): Promise<ResultadoRegistro> {
    await this.carregada;
    const idUsuario = this.sessao.usuario()?.id;
    if (!idUsuario) return { estado: 'recusado', motivo: 'Entre de novo para registrar.' };
    const id = novoIdCliente();
    const varias = Array.isArray(foto) ? foto : null;
    const fotos = varias ? varias.map((_, i) => `${id}-${i + 1}.jpg`) : undefined;
    const item: ItemFila = { ...novo, id, idUsuario, criadoEm: new Date().toISOString(), foto: fotos?.[0] ?? `${id}.jpg`, fotos, tentativas: 0 };
    if (varias) for (const [i, f] of varias.entries()) await this.guardado.gravarFoto(fotos![i], await reduzirFoto(f));
    else await this.guardado.gravarFoto(item.foto, await reduzirFoto(foto as Blob));
    this.itens.update((l) => [...l, item]);
    await this.salvar();
    await this.enviarTudo();
    if (this.respostas.has(id)) {
      const resposta = this.respostas.get(id);
      this.respostas.delete(id);
      return { estado: 'enviado', resposta };
    }
    const recusado = this.naoEnviados().find((i) => i.id === id);
    if (recusado) {
      await this.descartar(recusado);
      return { estado: 'recusado', motivo: recusado.motivo ?? 'O servidor não aceitou.' };
    }
    return { estado: 'guardado' };
  }

  /** Manda, na ordem, os registros de quem está logado. Para na primeira falha de rede e tenta de novo depois (espera crescente). */
  async enviarTudo(): Promise<void> {
    await this.carregada;
    const quem = this.sessao.usuario()?.id;
    if (this.enviando() || !this.rede.online() || !quem) return;
    this.enviando.set(true);
    try {
      for (const item of this.itens().filter((i) => i.idUsuario === quem))
        if ((await this.enviarUm(item)) === 'parar') {
          this.agendar();
          break;
        }
    } finally {
      this.enviando.set(false);
    }
  }

  /** Tira de "Não enviados" de vez (e apaga a foto guardada). Só quem fez o registro. */
  async descartar(item: ItemFila): Promise<void> {
    if (item.idUsuario !== this.sessao.usuario()?.id) return;
    this.naoEnviados.update((l) => l.filter((i) => i.id !== item.id));
    await this.apagarFotos(item);
    await this.salvar();
  }

  private async apagarFotos(item: ItemFila): Promise<void> {
    for (const nome of item.fotos ?? [item.foto]) await this.guardado.apagarFoto(nome);
  }

  /** Volta o registro de "Não enviados" para a fila (no fim) e tenta mandar. */
  async tentarDeNovo(item: ItemFila): Promise<void> {
    if (item.idUsuario !== this.sessao.usuario()?.id) return;
    this.naoEnviados.update((l) => l.filter((i) => i.id !== item.id));
    this.itens.update((l) => [...l, { ...item, motivo: undefined, tentativas: 0 }]);
    await this.salvar();
    await this.enviarTudo();
  }

  private async enviarUm(item: ItemFila): Promise<'ok' | 'recusado' | 'parar'> {
    const destino = DESTINO[item.tipo];
    try {
      // Cada foto sobe uma vez: no reenvio, as que já subiram não sobem de novo.
      const nomes = item.fotos ?? [item.foto];
      const arquivos = item.fotos ? (item.arquivos ??= []) : item.arquivo ? [item.arquivo] : [];
      for (const [i, nome] of nomes.entries()) {
        if (arquivos[i]) continue;
        const foto = await this.guardado.lerFoto(nome);
        if (!foto) return await this.recusar(item, 'A foto se perdeu antes do envio. Registre de novo.');
        const corpo = new FormData();
        corpo.append('foto', foto, nome);
        const { arquivo } = await firstValueFrom(this.http.post<{ arquivo: string }>(ambiente.api + destino.foto(item.alvo), corpo));
        arquivos[i] = arquivo;
        if (!item.fotos) item.arquivo = arquivo;
        await this.salvar();
      }
      const resposta = await firstValueFrom(
        this.http.post(ambiente.api + destino.registro(item.alvo), { ...corpoComFotos(item, arquivos), idCliente: item.id, registradoEm: item.criadoEm }),
      );
      this.respostas.set(item.id, resposta);
      this.itens.update((l) => l.filter((i) => i.id !== item.id));
      await this.apagarFotos(item);
      await this.salvar();
      return 'ok';
    } catch (e) {
      const status = e instanceof HttpErrorResponse ? e.status : 0;
      // Sem rede, servidor fora ou login vencido (o sistema pede a senha): fica na fila.
      if (status === 0 || status === 401 || status === 408 || status === 429 || status >= 500) {
        item.tentativas++;
        await this.salvar();
        return 'parar';
      }
      return await this.recusar(item, mensagemDeErro(e));
    }
  }

  private async recusar(item: ItemFila, motivo: string): Promise<'recusado'> {
    this.itens.update((l) => l.filter((i) => i.id !== item.id));
    this.naoEnviados.update((l) => [...l, { ...item, motivo }]);
    await this.salvar();
    return 'recusado';
  }

  private agendar(): void {
    clearTimeout(this.espera);
    const tentativas = Math.max(0, ...this.itens().map((i) => i.tentativas));
    this.espera = setTimeout(() => void this.enviarTudo(), Math.min(300_000, 5_000 * 2 ** Math.min(tentativas, 6)));
  }

  private async carregar(): Promise<void> {
    try {
      const salvo = JSON.parse((await this.guardado.ler(CHAVE)) ?? '{}') as { itens?: ItemFila[]; naoEnviados?: ItemFila[] };
      this.itens.set(salvo.itens ?? []);
      this.naoEnviados.set(salvo.naoEnviados ?? []);
    } catch {
      /* nada guardado */
    }
  }

  private async salvar(): Promise<void> {
    await this.guardado.gravar(CHAVE, JSON.stringify({ itens: this.itens(), naoEnviados: this.naoEnviados() }));
  }
}
