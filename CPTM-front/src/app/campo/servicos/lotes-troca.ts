import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { consulta } from '../../compartilhado/utilitarios/consulta';
import { LeituraDto } from '../../compartilhado/entidades/enums';
import { CompativelDto, LoteTrocaDto, NovoLoteTrocaDto, StatusLoteTroca } from '../entidades/campo';

/**
 * /api/lotes-troca (Victor, 2026-11-11): o técnico pede, quem aprova decide o lote inteiro e o pátio separa. O fechamento e a recusa da via
 * vão pela fila (nucleo/fila), com uma foto por dormente.
 */
@Injectable({ providedIn: 'root' })
export class LotesTroca {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/lotes-troca`;

  listar(status?: StatusLoteTroca | null): Observable<LoteTrocaDto[]> {
    return this.http.get<LoteTrocaDto[]>(this.url, { params: consulta({ status }) });
  }
  buscar(id: number): Observable<LoteTrocaDto> {
    return this.http.get<LoteTrocaDto>(`${this.url}/${id}`);
  }
  pedir(novo: NovoLoteTrocaDto): Observable<LoteTrocaDto> {
    return this.http.post<LoteTrocaDto>(this.url, novo);
  }
  /** Aprovar o lote inteiro (quantidade menor que a pedida exige o porquê em motivo) ou reprovar (com motivo). */
  decidir(id: number, aprovar: boolean, quantidade: number | null, motivo: string | null, urgente = false): Observable<LoteTrocaDto> {
    return this.http.post<LoteTrocaDto>(`${this.url}/${id}/decisao`, { aprovar, quantidade, motivo, urgente });
  }
  compativeis(id: number): Observable<CompativelDto[]> {
    return this.http.get<CompativelDto[]>(`${this.url}/${id}/compativeis`);
  }
  separar(id: number, idDormente: number, leitura: LeituraDto): Observable<LoteTrocaDto> {
    return this.http.post<LoteTrocaDto>(`${this.url}/${id}/separacao`, { idDormente, leitura });
  }
  decidirRecusa(id: number, decisao: 'Manter' | 'Aceitar', resposta: string | null): Observable<LoteTrocaDto> {
    return this.http.post<LoteTrocaDto>(`${this.url}/${id}/recusa/decisao`, { decisao, resposta });
  }
}
