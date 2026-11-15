import { InjectionToken } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { Directory, Filesystem } from '@capacitor/filesystem';
import { Preferences } from '@capacitor/preferences';

/**
 * Onde o app guarda o que precisa sobreviver sem rede e com o app fechado: no celular, os dados nas preferências do app e as fotos
 * na pasta do app; no navegador e nos testes, só na memória (guardar sem rede é coisa do app).
 */
export interface Guardado {
  /** Lê um texto pela chave (null quando não há nada guardado). */
  ler(chave: string): Promise<string | null>;
  /** Grava um texto na chave, por cima do que havia. */
  gravar(chave: string, valor: string): Promise<void>;
  /** Grava a foto com o nome dado (ex.: "<idCliente>.jpg"). */
  gravarFoto(nome: string, foto: Blob): Promise<void>;
  /** Lê a foto pelo nome (null quando ela não existe mais). */
  lerFoto(nome: string): Promise<Blob | null>;
  /** Apaga a foto; não reclama se ela já não existe. */
  apagarFoto(nome: string): Promise<void>;
}

/** A pasta do app onde ficam as fotos da fila. */
const PASTA = 'fila';

/** Passa a foto para texto (base64), que é como o celular grava o arquivo. */
const paraBase64 = (foto: Blob): Promise<string> =>
  new Promise((pronto, falhou) => {
    const leitor = new FileReader();
    leitor.onload = () => pronto(String(leitor.result).split(',')[1] ?? '');
    leitor.onerror = () => falhou(leitor.error);
    leitor.readAsDataURL(foto);
  });

/** No celular: os textos nas preferências do app e as fotos na pasta do app (continuam lá mesmo com o app fechado). */
class NoCelular implements Guardado {
  async ler(chave: string): Promise<string | null> {
    return (await Preferences.get({ key: chave })).value;
  }
  async gravar(chave: string, valor: string): Promise<void> {
    await Preferences.set({ key: chave, value: valor });
  }
  async gravarFoto(nome: string, foto: Blob): Promise<void> {
    // Cria a pasta na primeira foto.
    await Filesystem.writeFile({ path: `${PASTA}/${nome}`, data: await paraBase64(foto), directory: Directory.Data, recursive: true });
  }
  async lerFoto(nome: string): Promise<Blob | null> {
    try {
      const { data } = await Filesystem.readFile({ path: `${PASTA}/${nome}`, directory: Directory.Data });
      // No celular a foto volta como texto: vira imagem de novo.
      return typeof data === 'string' ? await (await fetch(`data:image/jpeg;base64,${data}`)).blob() : data;
    } catch {
      // A foto não existe mais.
      return null;
    }
  }
  async apagarFoto(nome: string): Promise<void> {
    // Apagar o que já não existe não é erro.
    await Filesystem.deleteFile({ path: `${PASTA}/${nome}`, directory: Directory.Data }).catch(() => undefined);
  }
}

/** No site e nos testes: tudo na memória (some ao recarregar a página). */
class NaMemoria implements Guardado {
  /** Os textos, pela chave. */
  private readonly dados = new Map<string, string>();
  /** As fotos, pelo nome. */
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

/** Qual guardado usar: no celular, o de verdade; fora dele, a memória. */
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
    // Quanto diminuir para o lado maior ficar com até 1600 px (nunca aumenta).
    const imagem = await createImageBitmap(foto);
    const escala = Math.min(1, ladoMaior / Math.max(imagem.width, imagem.height));
    // Já pequena e leve: fica como está.
    if (escala === 1 && foto.size < 1_500_000) return foto;
    // Redesenha no tamanho novo e salva em JPEG.
    const tela = document.createElement('canvas');
    tela.width = Math.round(imagem.width * escala);
    tela.height = Math.round(imagem.height * escala);
    tela.getContext('2d')!.drawImage(imagem, 0, 0, tela.width, tela.height);
    return await new Promise<Blob>((pronto) => tela.toBlob((b) => pronto(b ?? foto), 'image/jpeg', 0.8));
  } catch {
    // Não deu para diminuir (ou é teste): fica a original.
    return foto;
  }
}
