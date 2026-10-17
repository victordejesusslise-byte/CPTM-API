import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { consulta } from '../../compartilhado/utilitarios/consulta';

export interface ItemRelatorioDto { idEspecie: number; especie: string; volumeM3Estoque: number; volumeM3Entrada: number; volumeM3Saida: number; idadeMediaDias: number; }
export interface RelatorioDto { id: number; mesReferencia: string; status: 'Rascunho' | 'Enviado'; geradoEm: string; enviadoEm: string | null; volumeM3Estoque: number; itens: ItemRelatorioDto[]; }

/** /api/relatorios-ambientais (especialista e supervisor): gerar o rascunho do mês, enviar (congela) e a planilha do IBAMA. */
@Injectable({ providedIn: 'root' })
export class Relatorios {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/relatorios-ambientais`;

  listar(): Observable<RelatorioDto[]> {
    return this.http.get<RelatorioDto[]>(this.url);
  }
  gerar(mesIso: string): Observable<RelatorioDto> {
    return this.http.post<RelatorioDto>(this.url, null, { params: consulta({ mes: mesIso }) });
  }
  enviar(id: number): Observable<RelatorioDto> {
    return this.http.post<RelatorioDto>(`${this.url}/${id}/envio`, null);
  }
  planilha(id: number): Observable<Blob> {
    return this.http.get(`${this.url}/${id}/planilha`, { responseType: 'blob' });
  }
}