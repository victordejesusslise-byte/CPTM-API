import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';

export interface EtapaFluxoDto { etapa: number; nome: string; quemFaz: string; tabelas: string[]; linhas: number; feita: boolean; resumo: string; }

/** GET /api/apresentacao/fluxo: as etapas do caminho da peça com o que o banco tem em cada uma. */
@Injectable({ providedIn: 'root' })
export class Apresentacao {
  private readonly http = inject(HttpClient);

  fluxo(): Observable<EtapaFluxoDto[]> {
    return this.http.get<EtapaFluxoDto[]>(`${ambiente.api}/apresentacao/fluxo`);
  }
}
