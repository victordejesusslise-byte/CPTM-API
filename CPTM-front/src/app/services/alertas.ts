import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../environments/ambiente';
import { consulta } from '../helpers/consulta';
import { AlertaDto, AvisoTagDto, NovaVistoriaDto, NovoAvisoTagDto } from '../models/campo';
import { OrigemAlerta, StatusAlerta } from '../models/enums';

/** /api/alertas (técnico, operador da via, supervisor) e o aviso de tag sem cadastro. */
@Injectable({ providedIn: 'root' })
export class Alertas {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/alertas`;

  listar(origem?: OrigemAlerta | null, status?: StatusAlerta | null): Observable<AlertaDto[]> {
    return this.http.get<AlertaDto[]>(this.url, { params: consulta({ origem, status }) });
  }
  buscar(id: number): Observable<AlertaDto> {
    return this.http.get<AlertaDto>(`${this.url}/${id}`);
  }
  vistoriar(id: number, vistoria: NovaVistoriaDto): Observable<AlertaDto> {
    return this.http.post<AlertaDto>(`${this.url}/${id}/vistoria`, vistoria);
  }
  avisarTag(aviso: NovoAvisoTagDto): Observable<AvisoTagDto> {
    return this.http.post<AvisoTagDto>(`${ambiente.api}/tags/avisos`, aviso);
  }
}