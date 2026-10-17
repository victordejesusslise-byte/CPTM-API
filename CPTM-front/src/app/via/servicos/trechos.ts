import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { consulta } from '../../compartilhado/utilitarios/consulta';

export interface TrechoDoMapaDto { id: number; nome: string; linha: string; kmInicio: number; kmFim: number; pecas: number; ruimOuCritico: number; }
export interface PecaNoTrechoDto { id: number; codigo: string; tag: string | null; posicao: string | null; km: number; nota: string | null; }
export interface FaixaDoTrechoDto {
  kmInicio: number; kmFim: number; bom: number; regular: number; ruim: number; critico: number; semNota: number; pecas: PecaNoTrechoDto[];
}
export interface MapaDoTrechoDto { trecho: TrechoDoMapaDto; faixaM: number; faixas: FaixaDoTrechoDto[]; }

/** GET /api/via/trechos: a tela do trecho, com o ID de posição e a nota de cada peça assentada, por faixa de km. */
@Injectable({ providedIn: 'root' })
export class Trechos {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/via/trechos`;

  listar(): Observable<TrechoDoMapaDto[]> {
    return this.http.get<TrechoDoMapaDto[]>(this.url);
  }
  mapa(id: number, faixaM: number): Observable<MapaDoTrechoDto> {
    return this.http.get<MapaDoTrechoDto>(`${this.url}/${id}/mapa`, { params: consulta({ faixaM }) });
  }
}
