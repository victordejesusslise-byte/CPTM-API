import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, from, switchMap, timeout } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { reduzirFoto } from '../../nucleo/fila/guardado';

/**
 * Quanto o OCR espera o servidor: o operador tira a foto, espera e confere, e isso não passa de 45 s (Victor, 2026-11-15).
 * Sem resposta nesse tempo (o celular sem rede ou sem o Tailscale), a tela avisa em vez de carregar para sempre.
 */
export const ESPERA_DO_OCR_MS = 45_000;

export interface CampoOcrDto { id: number; campo: string; valorLido: string | null; confianca: number | null; conferePedido: boolean | null; pronto: boolean; valorConfirmado: string | null; corrigido: boolean | null; }
export interface LeituraOcrDto { id: number; fotoArquivo: string; textoLido: string | null; confiancaMedia: number | null; versaoModelo: number; confirmada: boolean; campos: CampoOcrDto[]; }

/** /api/ocr/placas (operador de pátio e supervisor): a foto da placa do feixe lida no servidor e a confirmação dos campos. */
@Injectable({ providedIn: 'root' })
export class Ocr {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/ocr/placas`;

  ler(foto: File, idPedido: number | null): Observable<LeituraOcrDto> {
    // A foto sobe reduzida (lado maior de 1600 px, como na fila): a de 12 MP do celular demorava para subir e para o servidor ler.
    return from(reduzirFoto(foto)).pipe(
      switchMap((menor) => {
        const corpo = new FormData();
        corpo.append('foto', menor, menor === foto ? foto.name : 'placa.jpg');
        if (idPedido) corpo.append('idPedido', String(idPedido));
        corpo.append('idCliente', crypto.randomUUID());
        return this.http.post<LeituraOcrDto>(this.url, corpo);
      }),
      timeout(ESPERA_DO_OCR_MS),
    );
  }
  /** As placas confirmadas do pedido que ainda não viraram feixe (a chegada do pedido já vem com elas). */
  doPedido(idPedido: number): Observable<LeituraOcrDto[]> {
    return this.http.get<LeituraOcrDto[]>(this.url, { params: { idPedido } });
  }
  confirmar(id: number, campos: { campo: string; valor: string }[]): Observable<LeituraOcrDto> {
    return this.http.post<LeituraOcrDto>(`${this.url}/${id}/confirmacao`, { campos }).pipe(timeout(ESPERA_DO_OCR_MS));
  }
}
