import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { consulta } from '../../compartilhado/utilitarios/consulta';
import { CompativelDto, TrocaDto } from '../entidades/campo';
import { LeituraDto, StatusTroca } from '../../compartilhado/entidades/enums';

/** /api/trocas: o pátio separa a peça compatível. A troca feita e a recusa da via vão pela fila (nucleo/fila). */
@Injectable({ providedIn: 'root' })
export class Trocas {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/trocas`;

  listar(status?: StatusTroca | null): Observable<TrocaDto[]> {
    return this.http.get<TrocaDto[]>(this.url, { params: consulta({ status }) });
  }
  buscar(id: number): Observable<TrocaDto> {
    return this.http.get<TrocaDto>(`${this.url}/${id}`);
  }
  compativeis(id: number): Observable<CompativelDto[]> {
    return this.http.get<CompativelDto[]>(`${this.url}/${id}/compativeis`);
  }
  separar(id: number, idDormente: number, leitura: LeituraDto): Observable<TrocaDto> {
    return this.http.post<TrocaDto>(`${this.url}/${id}/separacao`, { idDormente, leitura });
  }
  pedir(idDormente: number, idInspecao: number | null, motivo: string | null, km: number | null): Observable<TrocaDto> {
    return this.http.post<TrocaDto>(this.url, { idDormente, idInspecao, motivo, km });
  }
  decidir(id: number, aprovar: boolean, motivo: string | null): Observable<TrocaDto> {
    return this.http.post<TrocaDto>(this.url + '/' + id + '/decisao', { aprovar, motivo });
  }
  decidirRecusa(id: number, decisao: 'Manter' | 'Aceitar', resposta: string | null): Observable<TrocaDto> {
    return this.http.post<TrocaDto>(this.url + '/' + id + '/recusa/decisao', { decisao, resposta });
  }
}