import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';

export type TipoLaudo = 'recebimento' | 'inspecao' | 'liberacao' | 'troca';

/** O laudo como documento: o mesmo conteúdo do PDF, para a tela desenhar antes de baixar. */
export interface DocumentoLaudoDto {
  titulo: string;
  codigo: string;
  subtitulo: string;
  geradoEm: string;
  secoes: { titulo: string; campos: { rotulo: string; valor: string }[]; tabela: { colunas: string[]; linhas: string[][] } | null; fotos?: { legenda: string; caminho: string; tipo: string | null; conteudo: string | null }[] }[];
}

/** Os laudos: o documento para ver na tela e o PDF para baixar (vem como arquivo, com o token). */
@Injectable({ providedIn: 'root' })
export class Laudos {
  private readonly http = inject(HttpClient);

  documento(tipo: TipoLaudo, id: number): Observable<DocumentoLaudoDto> {
    return this.http.get<DocumentoLaudoDto>(`${ambiente.api}/laudos/${tipo}/${id}/documento`);
  }
  pdf(tipo: TipoLaudo, id: number): Observable<Blob> {
    return this.http.get(`${ambiente.api}/laudos/${tipo}/${id}`, { responseType: 'blob' });
  }
}
