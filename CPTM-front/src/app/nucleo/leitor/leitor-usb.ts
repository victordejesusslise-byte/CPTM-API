import { Injectable, InjectionToken, computed, inject, signal } from '@angular/core';
import { registerPlugin } from '@capacitor/core';
import { ChaveModulo, Leitor, LeituraDoModulo, MODOS_PADRAO, ResultadoGravacao, ResultadoLeitura, bipe, guardarUltimaLeitura } from './leitor';
import { Etiqueta, FIM, NUMERO_YBYRA, gravou, lerRead, lerScan, mensagemDoErro } from './protocolo-p0';

/**
 * O caminho até o módulo: o USB-C do celular, como serial (USB Host → CDC ACM → 115200). Só USB: o ESP32 não usa Wi-Fi nem Bluetooth
 * (Victor, 2026-11-12). No app, é o plugin nativo RfidUsb; nos testes, um firmware de mentira.
 */
export interface TransporteUsb {
  conectar(): Promise<void>;
  desconectar(): Promise<void>;
  enviar(linha: string): Promise<void>;
  aoLer(ouvinte: (linha: string) => void): void;
  aoMudar(ouvinte: (ligado: boolean) => void): void;
}

/** O plugin Android (android/app/src/main/java/br/cptm/ybyra/RfidUsbPlugin.java). */
interface RfidUsbNativo {
  conectar(): Promise<void>;
  desconectar(): Promise<void>;
  enviar(opcoes: { linha: string }): Promise<void>;
  addListener(evento: 'linha', ouvinte: (dados: { texto: string }) => void): Promise<unknown>;
  addListener(evento: 'conexao', ouvinte: (dados: { conectado: boolean }) => void): Promise<unknown>;
}

class TransportePlugin implements TransporteUsb {
  private readonly nativo = registerPlugin<RfidUsbNativo>('RfidUsb');
  conectar = () => this.nativo.conectar();
  desconectar = () => this.nativo.desconectar();
  enviar = (linha: string) => this.nativo.enviar({ linha });
  // Fora do Android (site, testes) o plugin não existe: sem eventos, e conectar diz para ligar o módulo.
  aoLer(ouvinte: (linha: string) => void): void {
    this.nativo.addListener('linha', (d) => ouvinte(d.texto)).catch(() => undefined);
  }
  aoMudar(ouvinte: (ligado: boolean) => void): void {
    this.nativo.addListener('conexao', (d) => ouvinte(d.conectado)).catch(() => undefined);
  }
}

export const TRANSPORTE_USB = new InjectionToken<TransporteUsb>('USB do módulo', { providedIn: 'root', factory: () => new TransportePlugin() });

/** O que o módulo viu por último (para o painel): o UID físico, o tipo e o número do YBYRA, se tinha. */
export type EtiquetaLida = Etiqueta & { id: string | null };

/** Quanto esperar cada resposta: o SCAN e o WRITE mexem na etiqueta e levam mais. */
const ESPERA_MS = { PING: 1500, SCAN: 2500, READ: 2500, WRITE: 5000 } as const;

/**
 * O módulo de verdade (protótipo YBYRA-RFID-P0, NFC 13,56 MHz): no lugar do simulado, no app, sem mudar as telas. Lê só de perto, então o
 * modo fica fixo no Normal e a leitura vai sem potência nem sinal (Victor, 2026-11-12: "só lê uma distância a normal"). A gravação só conta
 * com o VERIFY;OK do hardware e a releitura do número. Quando vier o YRM100 (UHF), troca este leitor, não as telas.
 */
@Injectable({ providedIn: 'root' })
export class LeitorUsb implements Leitor {
  private readonly usb = inject(TRANSPORTE_USB);
  readonly conectado = signal(false);
  readonly modo = computed(() => MODOS_PADRAO.N);
  /** O firmware que respondeu ao PING (ex.: YBYRA-RFID-P0). */
  readonly firmware = signal<string | null>(null);
  readonly etiqueta = signal<EtiquetaLida | null>(null);
  readonly ocupado = signal(false);
  private resposta: { linhas: string[]; fim: (l: string) => boolean; pronto: () => void } | null = null;

  constructor() {
    this.usb.aoLer((linha) => this.chegou(linha.trim()));
    this.usb.aoMudar((ligado) => {
      if (ligado && !this.conectado()) void this.conectar(); // o Android abriu a porta (módulo ligado no USB-C): confere com PING
      if (!ligado) {
        this.conectado.set(false);
        this.firmware.set(null);
      }
    });
  }

  private conectando: Promise<void> | null = null;

  /**
   * Conecta pelo USB-C (o Android pede a autorização na primeira vez) e confere com PING. O ESP32 pode reiniciar ao abrir: tenta de novo.
   * Uma conexão por vez: o aviso de porta aberta, que chega no meio dela, não começa outra.
   */
  conectar(): Promise<void> {
    // Começa no próximo passo: a conexão fica registrada antes de o aviso de porta aberta (que vem dentro dela) chegar.
    return (this.conectando ??= Promise.resolve().then(() => this.fazerConexao()).finally(() => (this.conectando = null)));
  }

  private async fazerConexao(): Promise<void> {
    try {
      await this.usb.conectar();
    } catch {
      this.conectado.set(false);
      return;
    }
    for (let tentativa = 0; tentativa < 3 && !this.conectado(); tentativa++) {
      const pong = (await this.comando('PING', 'PING')).find((l) => l.startsWith('PONG;'));
      if (pong) {
        this.firmware.set(pong.slice(5));
        this.conectado.set(true);
      }
    }
  }

  desconectar(): void {
    void this.usb.desconectar();
    this.conectado.set(false);
  }

  /** No protótipo não há chave física de modo: fica no Normal. */
  trocarModo(_chave: ChaveModulo): void {
    /* o P0 lê só na distância normal */
  }

  /** SCAN e READ: o número do YBYRA da etiqueta perto do módulo. */
  async ler(): Promise<ResultadoLeitura> {
    const r = await this.identificar();
    if ('motivo' in r) return this.falhou(r.motivo);
    if (!r.id) return this.falhou('A etiqueta não tem número do YBYRA gravado. Se ela é nova, grave o número no cadastro da peça.');
    const leitura: LeituraDoModulo = { tag: r.id, idModo: this.modo().idModo, potenciaDbm: null, rssiDbm: null, tagsVistas: 1 };
    guardarUltimaLeitura(leitura);
    bipe(true);
    return { ok: true, leitura };
  }

  /** Grava o número que o sistema deu (nunca um inventado aqui): SCAN, WRITE (só vale com VERIFY;OK) e READ para conferir. */
  async gravarEReler(numero: string): Promise<ResultadoGravacao> {
    const tag = numero.trim();
    if (!NUMERO_YBYRA.test(tag)) return this.falhou('O número precisa ter BR e 10 números (ex.: BR0000000032).');
    if (!(await this.pronto())) return this.falhou('Conecte o módulo no celular.');
    const scan = lerScan(await this.comando('SCAN', 'SCAN'));
    if ('erro' in scan) return this.falhou(mensagemDoErro(scan.erro));
    if (!scan.gravavel) return this.falhou(mensagemDoErro('UID_NAO_AUTORIZADO_PARA_ESCRITA'));
    const escrita = await this.comando(`WRITE ${tag}`, 'WRITE');
    if (!gravou(escrita, tag)) {
      const erro = escrita.filter((l) => l.startsWith('ERRO;')).pop()?.slice(5).split(/[;=]/)[0];
      return this.falhou(mensagemDoErro(erro ?? 'WRITE_OU_VERIFY_FALHOU'));
    }
    const releitura = lerRead(await this.comando('READ', 'READ'));
    if (!('id' in releitura) || releitura.id !== tag) return this.falhou(mensagemDoErro('WRITE_OU_VERIFY_FALHOU'));
    this.etiqueta.set({ ...scan, id: releitura.id });
    bipe(true);
    return { ok: true, relida: releitura.id };
  }

  /** O botão do painel: o UID físico, o tipo e o número do YBYRA (SCAN e READ), ou o porquê não leu. */
  async identificar(): Promise<EtiquetaLida | { motivo: string }> {
    if (!(await this.pronto())) return { motivo: 'Conecte o módulo no celular.' };
    const scan = lerScan(await this.comando('SCAN', 'SCAN'));
    if ('erro' in scan) return { motivo: mensagemDoErro(scan.erro) };
    const read = lerRead(await this.comando('READ', 'READ'));
    if ('erro' in read) return { motivo: mensagemDoErro(read.erro) };
    const lida: EtiquetaLida = { ...scan, id: 'id' in read ? read.id : null };
    this.etiqueta.set(lida);
    return lida;
  }

  private async pronto(): Promise<boolean> {
    if (!this.conectado()) await this.conectar();
    return this.conectado();
  }

  private falhou(motivo: string): { ok: false; motivo: string } {
    bipe(false);
    return { ok: false, motivo };
  }

  /** Manda um comando e junta as linhas até a última da resposta dele (ou até o tempo acabar: volta o que chegou). Um comando por vez. */
  private async comando(linha: string, tipo: keyof typeof FIM): Promise<string[]> {
    while (this.resposta) await new Promise((r) => setTimeout(r, 20));
    this.ocupado.set(true);
    const linhas: string[] = [];
    try {
      await new Promise<void>((pronto) => {
        const tempo = setTimeout(pronto, ESPERA_MS[tipo]);
        this.resposta = { linhas, fim: FIM[tipo], pronto: () => (clearTimeout(tempo), pronto()) };
        this.usb.enviar(linha).catch(() => (clearTimeout(tempo), pronto()));
      });
    } finally {
      this.resposta = null;
      this.ocupado.set(false);
    }
    if (!linhas.length) linhas.push('ERRO;SEM_RESPOSTA');
    return linhas;
  }

  private chegou(linha: string): void {
    const r = this.resposta;
    if (!r || !linha) return;
    r.linhas.push(linha);
    if (r.fim(linha)) r.pronto();
  }
}
