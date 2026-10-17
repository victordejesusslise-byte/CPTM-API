import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { ArmazemDto, EspecieDto, LeitorDto, ModoLeituraDto, TamanhoDto } from '../entidades/catalogo';

export interface NovoLeitorDto { idTipoLeitor: number; codigo: string; numeroSerie?: string | null; idSetor?: number | null; }

/** /api/catalogos: tamanhos, espécies, armazéns, leitores e modos de leitura. Tamanhos e espécies quase não mudam: ficam guardados. */
@Injectable({ providedIn: 'root' })
export class Catalogos {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/catalogos`;
  private tamanhosGuardados?: Observable<TamanhoDto[]>;
  private especiesGuardadas?: Observable<EspecieDto[]>;

  tamanhos(): Observable<TamanhoDto[]> {
    return (this.tamanhosGuardados ??= this.http.get<TamanhoDto[]>(`${this.url}/tamanhos`).pipe(shareReplay(1)));
  }

  especies(): Observable<EspecieDto[]> {
    return (this.especiesGuardadas ??= this.http.get<EspecieDto[]>(`${this.url}/especies`).pipe(shareReplay(1)));
  }

  armazens(): Observable<ArmazemDto[]> {
    return this.http.get<ArmazemDto[]>(`${this.url}/armazens`);
  }

  leitores(): Observable<LeitorDto[]> {
    return this.http.get<LeitorDto[]>(`${this.url}/leitores`);
  }

  modosLeitura(): Observable<ModoLeituraDto[]> {
    return this.http.get<ModoLeituraDto[]>(`${this.url}/modos-leitura`);
  }

  novoLeitor(novo: NovoLeitorDto): Observable<LeitorDto> {
    return this.http.post<LeitorDto>(`${this.url}/leitores`, novo);
  }
}