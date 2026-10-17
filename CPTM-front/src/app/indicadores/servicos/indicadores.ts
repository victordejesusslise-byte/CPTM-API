import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { consulta } from '../../compartilhado/utilitarios/consulta';
import { IndicadorDto } from '../entidades/indicador';

/** GET /api/indicadores: o painel do supervisor. Sem período, a API usa o padrão dela. */
@Injectable({ providedIn: 'root' })
export class Indicadores {
  private readonly http = inject(HttpClient);

  listar(de?: string | null, ate?: string | null): Observable<IndicadorDto[]> {
    return this.http.get<IndicadorDto[]>(`${ambiente.api}/indicadores`, { params: consulta({ de, ate }) });
  }
}