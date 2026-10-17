import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';

export type TipoLaudo = 'recebimento' | 'inspecao' | 'liberacao' | 'troca';

/** Os laudos em PDF. Vêm como arquivo (com o token), então a tela abre com helpers/arquivo. */
@Injectable({ providedIn: 'root' })
export class Laudos {
  private readonly http = inject(HttpClient);

  pdf(tipo: TipoLaudo, id: number): Observable<Blob> {
    return this.http.get(`${ambiente.api}/laudos/${tipo}/${id}`, { responseType: 'blob' });
  }
}