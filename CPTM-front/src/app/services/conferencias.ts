import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../environments/ambiente';
import { ConferenciaDto } from '../models/peca';

/** A conferência do armazém: abre, lê pilha por pilha e conclui com o que falta e o que sobra. */
@Injectable({ providedIn: 'root' })
export class Conferencias {
  private readonly http = inject(HttpClient);
  private readonly api = ambiente.api;

  iniciar(idArmazem: number): Observable<ConferenciaDto> {
    return this.http.post<ConferenciaDto>(`${this.api}/armazens/${idArmazem}/conferencias`, null);
  }
  buscar(id: number): Observable<ConferenciaDto> {
    return this.http.get<ConferenciaDto>(`${this.api}/conferencias/${id}`);
  }
  lerPilha(id: number, idPilha: number, tags: string[]): Observable<ConferenciaDto> {
    return this.http.post<ConferenciaDto>(`${this.api}/conferencias/${id}/pilhas`, { idPilha, tags });
  }
  concluir(id: number): Observable<ConferenciaDto> {
    return this.http.post<ConferenciaDto>(`${this.api}/conferencias/${id}/conclusao`, null);
  }
}