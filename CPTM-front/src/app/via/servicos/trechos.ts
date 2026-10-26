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

/**
 * As faixas do mapa montadas no celular, sem rede, do mesmo jeito do servidor (TrechoMapaService.Faixas): faixas de faixaM metros do
 * começo ao fim do trecho (a peça fora dos km das estações estica o começo ou o fim), até 400. A conta é em metros, para não errar na vírgula.
 */
export const faixasDoTrecho = (kmInicio: number, kmFim: number, faixaM: number, pecas: PecaNoTrechoDto[]): FaixaDoTrechoDto[] => {
  const m = (km: number) => Math.round(km * 1000);
  const kms = [m(kmInicio), m(kmFim), ...pecas.map((p) => m(p.km))];
  const comeco = Math.floor(Math.min(...kms) / faixaM) * faixaM;
  const quantas = Math.min(Math.max(Math.ceil((Math.max(...kms) - comeco) / faixaM), 1), 400);
  const faixas: FaixaDoTrechoDto[] = Array.from({ length: quantas }, (_, i) => ({
    kmInicio: (comeco + i * faixaM) / 1000, kmFim: (comeco + (i + 1) * faixaM) / 1000, bom: 0, regular: 0, ruim: 0, critico: 0, semNota: 0, pecas: [],
  }));
  for (const p of [...pecas].sort((a, b) => a.km - b.km)) {
    const f = faixas[Math.min(Math.max(Math.floor((m(p.km) - comeco) / faixaM), 0), quantas - 1)];
    f.pecas.push(p);
    if (p.nota === 'Bom') f.bom++;
    else if (p.nota === 'Regular') f.regular++;
    else if (p.nota === 'Ruim') f.ruim++;
    else if (p.nota === 'Crítico') f.critico++;
    else f.semNota++;
  }
  return faixas;
};

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
