import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../environments/ambiente';

export interface NotaImportadaDto { pedido: string; numero: string; idNota: number; statusConferencia: string; divergencias: string[]; }
export interface ImportacaoNotasDto { arquivo: string; notas: NotaImportadaDto[]; erros: string[]; }
export interface CodigoMaterialDto { id: number; idTamanho: number; comprimentoM: number; classe: number; codigoErp: string; }
export interface IntegracaoLogDto { id: number; data: string; sistema: string; direcao: string; tipo: string; status: string; mensagem: string | null; arquivo: string | null; }

/** /api/integracoes (supervisor e operador de pátio): notas pela planilha, estoque para o ERP e o histórico. */
@Injectable({ providedIn: 'root' })
export class Integracoes {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/integracoes`;

  importarNotas(arquivo: File): Observable<ImportacaoNotasDto> {
    const corpo = new FormData();
    corpo.append('arquivo', arquivo);
    return this.http.post<ImportacaoNotasDto>(`${this.url}/notas`, corpo);
  }
  modeloNotas(): Observable<Blob> {
    return this.http.get(`${this.url}/notas/modelo`, { responseType: 'blob' });
  }
  estoqueErp(): Observable<Blob> {
    return this.http.get(`${this.url}/erp/estoque`, { responseType: 'blob' });
  }
  codigosMaterial(): Observable<CodigoMaterialDto[]> {
    return this.http.get<CodigoMaterialDto[]>(`${this.url}/codigos-material`);
  }
  novoCodigoMaterial(idTamanho: number, classe: number, codigoErp: string): Observable<CodigoMaterialDto> {
    return this.http.post<CodigoMaterialDto>(`${this.url}/codigos-material`, { idTamanho, classe, codigoErp });
  }
  log(): Observable<IntegracaoLogDto[]> {
    return this.http.get<IntegracaoLogDto[]>(`${this.url}/log`);
  }
}