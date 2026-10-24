import { InjectionToken } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { Directory, Filesystem } from '@capacitor/filesystem';
import { Preferences } from '@capacitor/preferences';

/**
 * Onde o app guarda o que precisa sobreviver sem rede e com o app fechado: no celular, os dados nas preferências do app e as fotos
 * na pasta do app; no navegador e nos testes, só na memória (guardar sem rede é coisa do app).
 */
export interface Guardado {
  ler(chave: string): Promise<string | null>;
  gravar(chave: string, valor: string): Promise<void>;
  gravarFoto(nome: string, foto: Blob): Promise<void>;
  lerFoto(nome: string): Promise<Blob | null>;
  apagarFoto(nome: string): Promise<void>;
}

const PASTA = 'fila';

const paraBase64 = (foto: Blob): Promise<string> =>
  new Promise((pronto, falhou) => {
    const leitor = new FileReader();
    leitor.onload = () => pronto(String(leitor.result).split(',')[1] ?? '');
    leitor.onerror = () => falhou(leitor.error);
    leitor.readAsDataURL(foto);
  });

class NoCelular implements Guardado {
  async ler(chave: string): Promise<string | null> {
    return (await Preferences.get({ key: chave })).value;
  }
  async gravar(chave: string, valor: string): Promise<void> {
    await Preferences.set({ key: chave, value: valor });
  }
  async gravarFoto(nome: string, foto: Blob): Promise<void> {
    await Filesystem.writeFile({ path: `${PASTA}/${nome}`, data: await paraBase64(foto), directory: Directory.Data, recursive: true });
  }
  async lerFoto(nome: string): Promise<Blob | null> {
    try {
      const { data } = await Filesystem.readFile({ path: `${PASTA}/${nome}`, directory: Directory.Data });
      return typeof data === 'string' ? await (await fetch(`data:image/jpeg;base64,${data}`)).blob() : data;
    } catch {
      return null;
    }
  }
  async apagarFoto(nome: string): Promise<void> {
    await Filesystem.deleteFile({ path: `${PASTA}/${nome}`, directory: Directory.Data }).catch(() => undefined);
  }
}

class NaMemoria implements Guardado {
  private readonly dados = new Map<string, string>();
  private readonly fotos = new Map<string, Blob>();
  async ler(chave: string): Promise<string | null> {
    return this.dados.get(chave) ?? null;
  }
  async gravar(chave: string, valor: string): Promise<void> {
    this.dados.set(chave, valor);
  }
  async gravarFoto(nome: string, foto: Blob): Promise<void> {
    this.fotos.set(nome, foto);
  }
  async lerFoto(nome: string): Promise<Blob | null> {
    return this.fotos.get(nome) ?? null;
  }
  async apagarFoto(nome: string): Promise<void> {
    this.fotos.delete(nome);
  }
}

export const GUARDADO = new InjectionToken<Guardado>('Guardado no aparelho', {
  providedIn: 'root',
  factory: () => (Capacitor.isNativePlatform() ? new NoCelular() : new NaMemoria()),
});

/**
 * Diminui a foto antes de guardar e de mandar (lado maior até 1600 px, JPEG 80%): cabe mais no celular e sobe mais rápido na rede fraca.
 * Se o aparelho não consegue (ou nos testes), fica a original.
 */
export async function reduzirFoto(foto: Blob, ladoMaior = 1600): Promise<Blob> {
  try {
    const imagem = await createImageBitmap(foto);
    const escala = Math.min(1, ladoMaior / Math.max(imagem.width, imagem.height));
    if (escala === 1 && foto.size < 1_500_000) return foto;
    const tela = document.createElement('canvas');
    tela.width = Math.round(imagem.width * escala);
    tela.height = Math.round(imagem.height * escala);
    tela.getContext('2d')!.drawImage(imagem, 0, 0, tela.width, tela.height);
    return await new Promise<Blob>((pronto) => tela.toBlob((b) => pronto(b ?? foto), 'image/jpeg', 0.8));
  } catch {
    return foto;
  }
}
