import { Injectable, InjectionToken, computed, inject, signal } from '@angular/core';
import { registerPlugin } from '@capacitor/core';
import { ChaveModulo, Leitor, LeituraDoModulo, MODOS_PADRAO, ResultadoGravacao, ResultadoLeitura, bipe, guardarUltimaLeitura } from './leitor';
import { Etiqueta, FIM, NUMERO_YBYRA, gravou, lerRead, lerScan, mensagemDoErro } from './protocolo-p0';

/**
 * O caminho até o módulo: o USB-C do celular, como serial (USB Host → CDC ACM → 115200). Só USB: o ESP32 não usa Wi-Fi nem Bluetooth
 * (Victor, 2026-11-11). No app, é o plugin nativo RfidUsb; nos testes, um firmware de mentira.
 */
export interface TransporteUsb {
  /** Abre a porta USB do módulo. */
  conectar(): Promise<void>;
  /** Fecha a porta. */
  desconectar(): Promise<void>;
  /** Manda uma linha de comando ao módulo (ex.: "SCAN"). */
  enviar(linha: string): Promise<void>;
  /** Avisa cada linha que o módulo responde. */
  aoLer(ouvinte: (linha: string) => void): void;
  /** Avisa quando o módulo é ligado ou tirado do USB-C. */
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

/** O transporte de verdade: repassa tudo para o plugin Android RfidUsb. */
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

/** O transporte que o leitor usa (os testes trocam por um firmware de mentira). */
export const TRANSPORTE_USB = new InjectionToken<TransporteUsb>('USB do módulo', { providedIn: 'root', factory: () => new TransportePlugin() });

/** O que o módulo viu por último (para o painel): o UID físico, o tipo e o número do YBYRA, se tinha. */
export type EtiquetaLida = Etiqueta & { id: string | null };

/** Quanto esperar cada resposta: o SCAN e o WRITE mexem na etiqueta e levam mais. */
const ESPERA_MS = { PING: 1500, SCAN: 2500, READ: 2500, WRITE: 5000 } as const;

/**
 * O módulo de verdade (protótipo YBYRA-RFID-P0, NFC 13,56 MHz): no lugar do simulado, no app, sem mudar as telas. Lê só de perto, então o
 * modo fica fixo no Normal e a leitura vai sem potência nem sinal (Victor, 2026-11-11: "só lê uma distância a normal"). A gravação só conta
 * com o VERIFY;OK do hardware e a releitura do número. Quando vier o YRM100 (UHF), troca este leitor, não as telas.
 */
@Injectable({ providedIn: 'root' })
export class LeitorUsb implements Leitor {
  private readonly usb = inject(TRANSPORTE_USB);
  /** O módulo respondeu ao PING? */
  readonly conectado = signal(false);
  /** Sempre o modo Normal: o protótipo só lê de perto. */
  readonly modo = computed(() => MODOS_PADRAO.N);
  /** O firmware que respondeu ao PING (ex.: YBYRA-RFID-P0). */
  readonly firmware = signal<string | null>(null);
  /** A última etiqueta que o módulo viu (o painel mostra). */
  readonly etiqueta = signal<EtiquetaLida | null>(null);
  /** Um comando está esperando resposta? (o painel trava os botões) */
  readonly ocupado = signal(false);
  /** O comando que espera resposta agora: as linhas que já chegaram e como saber que acabou. */
  private resposta: { linhas: string[]; fim: (l: string) => boolean; pronto: () => void } | null = null;

  constructor() {
    // Cada linha que o módulo manda vai para o comando que espera.
    this.usb.aoLer((linha) => this.chegou(linha.trim()));
    // Módulo ligado: conecta. Módulo tirado: marca como desconectado.
    this.usb.aoMudar((ligado) => {
      if (ligado && !this.conectado()) void this.conectar(); // o Android abriu a porta (módulo ligado no USB-C): confere com PING
      if (!ligado) {
        this.conectado.set(false);
        this.firmware.set(null);
      }
    });
  }

  /** A conexão em andamento (para não começar duas). */
  private conectando: Promise<void> | null = null;

  /**
   * Conecta pelo USB-C (o Android pede a autorização na primeira vez) e confere com PING. O ESP32 pode reiniciar ao abrir: tenta de novo.
   * Uma conexão por vez: o aviso de porta aberta, que chega no meio dela, não começa outra.
   */
  conectar(): Promise<void> {
    // Começa no próximo passo: a conexão fica registrada antes de o aviso de porta aberta (que vem dentro dela) chegar.
    return (this.conectando ??= Promise.resolve().then(() => this.fazerConexao()).finally(() => (this.conectando = null)));
  }

  /** Abre a porta e manda PING até 3 vezes; quem responde PONG está conectado. */
  private async fazerConexao(): Promise<void> {
    // A porta não abriu (sem módulo, ou o Android não autorizou).
    try {
      await this.usb.conectar();
    } catch {
      this.conectado.set(false);
      return;
    }
    // O ESP32 pode reiniciar ao abrir a porta: por isso até 3 tentativas.
    for (let tentativa = 0; tentativa < 3 && !this.conectado(); tentativa++) {
      const pong = (await this.comando('PING', 'PING')).find((l) => l.startsWith('PONG;'));
      if (pong) {
        this.firmware.set(pong.slice(5));
        this.conectado.set(true);
      }
    }
  }

  /** Fecha a porta USB. */
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
    // Etiqueta nova, ainda sem número: o número é gravado no cadastro da peça.
    if (!r.id) return this.falhou('A etiqueta não tem número do YBYRA gravado. Se ela é nova, grave o número no cadastro da peça.');
    // Leu: guarda a leitura para ir junto à API e apita.
    const leitura: LeituraDoModulo = { tag: r.id, idModo: this.modo().idModo, potenciaDbm: null, rssiDbm: null, tagsVistas: 1 };
    guardarUltimaLeitura(leitura);
    bipe(true);
    return { ok: true, leitura };
  }

  /** Grava o número que o sistema deu (nunca um inventado aqui): SCAN, WRITE (só vale com VERIFY;OK) e READ para conferir. */
  async gravarEReler(numero: string): Promise<ResultadoGravacao> {
    // O número tem de ser BR + 10 dígitos, e o módulo tem de estar ligado.
    const tag = numero.trim();
    if (!NUMERO_YBYRA.test(tag)) return this.falhou('O número precisa ter BR e 10 números (ex.: BR0000000032).');
    if (!(await this.pronto())) return this.falhou('Conecte o módulo no celular.');
    // 1. SCAN: tem uma etiqueta à frente, e ela pode ser gravada?
    const scan = lerScan(await this.comando('SCAN', 'SCAN'));
    if ('erro' in scan) return this.falhou(mensagemDoErro(scan.erro));
    if (!scan.gravavel) return this.falhou(mensagemDoErro('UID_NAO_AUTORIZADO_PARA_ESCRITA'));
    // 2. WRITE: só vale com a confirmação do próprio módulo (VERIFY;OK).
    const escrita = await this.comando(`WRITE ${tag}`, 'WRITE');
    if (!gravou(escrita, tag)) {
      // Mostra o último erro que o módulo mandou.
      const erro = escrita.filter((l) => l.startsWith('ERRO;')).pop()?.slice(5).split(/[;=]/)[0];
      return this.falhou(mensagemDoErro(erro ?? 'WRITE_OU_VERIFY_FALHOU'));
    }
    // 3. READ: relê e confere se ficou o mesmo número.
    const releitura = lerRead(await this.comando('READ', 'READ'));
    if (!('id' in releitura) || releitura.id !== tag) return this.falhou(mensagemDoErro('WRITE_OU_VERIFY_FALHOU'));
    this.etiqueta.set({ ...scan, id: releitura.id });
    bipe(true);
    return { ok: true, relida: releitura.id };
  }

  /** O botão do painel: o UID físico, o tipo e o número do YBYRA (SCAN e READ), ou o porquê não leu. */
  async identificar(): Promise<EtiquetaLida | { motivo: string }> {
    if (!(await this.pronto())) return { motivo: 'Conecte o módulo no celular.' };
    // SCAN acha a etiqueta; READ lê o número gravado nela.
    const scan = lerScan(await this.comando('SCAN', 'SCAN'));
    if ('erro' in scan) return { motivo: mensagemDoErro(scan.erro) };
    const read = lerRead(await this.comando('READ', 'READ'));
    if ('erro' in read) return { motivo: mensagemDoErro(read.erro) };
    // Sem número do YBYRA, o id fica nulo.
    const lida: EtiquetaLida = { ...scan, id: 'id' in read ? read.id : null };
    this.etiqueta.set(lida);
    return lida;
  }

  /** Se não está conectado, tenta conectar antes; diz se ficou pronto. */
  private async pronto(): Promise<boolean> {
    if (!this.conectado()) await this.conectar();
    return this.conectado();
  }

  /** Apita o "não leu" e devolve o motivo. */
  private falhou(motivo: string): { ok: false; motivo: string } {
    bipe(false);
    return { ok: false, motivo };
  }

  /** Manda um comando e junta as linhas até a última da resposta dele (ou até o tempo acabar: volta o que chegou). Um comando por vez. */
  private async comando(linha: string, tipo: keyof typeof FIM): Promise<string[]> {
    // Espera o comando anterior acabar.
    while (this.resposta) await new Promise((r) => setTimeout(r, 20));
    this.ocupado.set(true);
    const linhas: string[] = [];
    try {
      // Manda e espera a última linha da resposta, ou o tempo acabar.
      await new Promise<void>((pronto) => {
        const tempo = setTimeout(pronto, ESPERA_MS[tipo]);
        this.resposta = { linhas, fim: FIM[tipo], pronto: () => (clearTimeout(tempo), pronto()) };
        this.usb.enviar(linha).catch(() => (clearTimeout(tempo), pronto()));
      });
    } finally {
      this.resposta = null;
      this.ocupado.set(false);
    }
    // Nada chegou: vira o erro "sem resposta".
    if (!linhas.length) linhas.push('ERRO;SEM_RESPOSTA');
    return linhas;
  }

  /** Uma linha do módulo chegou: junta na resposta e, se for a última, libera o comando. */
  private chegou(linha: string): void {
    const r = this.resposta;
    if (!r || !linha) return;
    r.linhas.push(linha);
    if (r.fim(linha)) r.pronto();
  }
}
