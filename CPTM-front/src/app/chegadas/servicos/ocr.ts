import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';

export interface CampoOcrDto { id: number; campo: string; valorLido: string | null; confianca: number | null; conferePedido: boolean | null; pronto: boolean; valorConfirmado: string | null; corrigido: boolean | null; }
export interface LeituraOcrDto { id: number; fotoArquivo: string; textoLido: string | null; confiancaMedia: number | null; versaoModelo: number; confirmada: boolean; campos: CampoOcrDto[]; }

/** /api/ocr/placas (operador de pátio e supervisor): a foto da placa do feixe lida no servidor e a confirmação dos campos. */
@Injectable({ providedIn: 'root' })
export class Ocr {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/ocr/placas`;

  ler(foto: File, idPedido: number | null): Observable<LeituraOcrDto> {
    const corpo = new FormData();
    corpo.append('foto', foto);
    if (idPedido) corpo.append('idPedido', String(idPedido));
    corpo.append('idCliente', crypto.randomUUID());
    return this.http.post<LeituraOcrDto>(this.url, corpo);
  }
  confirmar(id: number, campos: { campo: string; valor: string }[]): Observable<LeituraOcrDto> {
    return this.http.post<LeituraOcrDto>(`${this.url}/${id}/confirmacao`, { campos });
  }
}