import { Injectable, signal } from '@angular/core';
import { Network } from '@capacitor/network';

/**
 * Tem rede? No app, pelo aviso do Android; no navegador, pelo do navegador (o plugin cuida dos dois). Ter rede não garante chegar ao
 * servidor (o de homologação só no mesmo Wi-Fi): a fila trata a falha de envio como "sem rede" e tenta de novo.
 */
@Injectable({ providedIn: 'root' })
export class Rede {
  /** Tem rede agora? Começa com o que o navegador sabe. */
  readonly online = signal(typeof navigator === 'undefined' ? true : navigator.onLine);

  constructor() {
    // Pergunta o estado uma vez e depois escuta cada mudança.
    Network.getStatus()
      .then((s) => this.online.set(s.connected))
      .catch(() => undefined);
    Network.addListener('networkStatusChange', (s) => this.online.set(s.connected)).catch(() => undefined);
  }
}
