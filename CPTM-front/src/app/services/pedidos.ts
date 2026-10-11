import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../environments/ambiente';
import { consulta } from '../helpers/consulta';
import {
  ChegadaDto, NotaDto, NovaChegadaDto, NovaNotaDto, NovoPedidoDto, NovoPedidoItemDto, OfertaDto, PedidoDto, PedidoHistoricoDto, VerificacaoPedidoDto,
} from '../models/pedido';

/** /api/pedidos com as notas e as chegadas de cada pedido. */
@Injectable({ providedIn: 'root' })
export class Pedidos {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/pedidos`;

  listar(): Observable<PedidoDto[]> {
    return this.http.get<PedidoDto[]>(this.url);
  }
  buscar(id: number): Observable<PedidoDto> {
    return this.http.get<PedidoDto>(`${this.url}/${id}`);
  }
  criar(novo: NovoPedidoDto): Observable<PedidoDto> {
    return this.http.post<PedidoDto>(this.url, novo);
  }
  /** Sem gravar: a regra da 2ª classe (bloqueia) e a oferta do fornecedor (só avisa). */
  verificar(idEmpresa: number | null, itens: NovoPedidoItemDto[]): Observable<VerificacaoPedidoDto> {
    return this.http.post<VerificacaoPedidoDto>(`${this.url}/verificacao`, { idEmpresa, itens });
  }
  /** Só antes da primeira chegada, com motivo; os itens vêm completos e substituem os de antes. */
  corrigir(id: number, correcao: { idContrato: number | null; dataCompra: string | null; itens: NovoPedidoItemDto[]; motivo: string }): Observable<PedidoDto> {
    return this.http.post<PedidoDto>(`${this.url}/${id}/correcao`, correcao);
  }
  ofertas(idEmpresa: number): Observable<OfertaDto[]> {
    return this.http.get<OfertaDto[]>(`${ambiente.api}/ofertas`, { params: consulta({ idEmpresa }) });
  }
  cancelar(id: number, motivo: string): Observable<PedidoDto> {
    return this.http.post<PedidoDto>(`${this.url}/${id}/cancelamento`, { motivo });
  }
  historico(id: number): Observable<PedidoHistoricoDto[]> {
    return this.http.get<PedidoHistoricoDto[]>(`${this.url}/${id}/historico`);
  }
  notas(id: number): Observable<NotaDto[]> {
    return this.http.get<NotaDto[]>(`${this.url}/${id}/notas`);
  }
  novaNota(id: number, nota: NovaNotaDto): Observable<NotaDto> {
    return this.http.post<NotaDto>(`${this.url}/${id}/notas`, nota);
  }
  chegadas(id: number): Observable<ChegadaDto[]> {
    return this.http.get<ChegadaDto[]>(`${this.url}/${id}/chegadas`);
  }
  novaChegada(id: number, chegada: NovaChegadaDto): Observable<ChegadaDto> {
    return this.http.post<ChegadaDto>(`${this.url}/${id}/chegadas`, chegada);
  }
}