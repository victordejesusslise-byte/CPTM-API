import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { consulta } from '../../compartilhado/utilitarios/consulta';

export interface PassagemDto {
  id: number; codigo: string; dataInicio: string; dataFim: string | null; leituras: number; semResposta: number; foraDePosicao: number;
  tagsDesconhecidas: number; mudancasDeFaixa: number; alertasSensor: number; alertasImagem: number; deteccoesAbaixoDoMinimo: number;
}
export interface LeituraDaPassagemDto { id: number; idDormente: number | null; codigoPeca: string | null; km: number | null; leitura: boolean; desvioMm: number | null; rachaduraSuspeita: boolean; foraDePosicao: boolean; data: string; notaPeca?: string | null; latitude?: number | null; longitude?: number | null; }
/** estacaoInicio e estacaoFim: as estações da passagem (o bip 0 e o fim); pontosGps: quantos pontos o caminho do GPS tem. Vazios nas passagens sem GPS. */
export interface PassagemDetalheDto { resumo: PassagemDto; veiculo: string | null; kmInicio: number | null; kmFim: number | null; limiteMm: number; leituras: LeituraDaPassagemDto[]; estacaoInicio?: string | null; estacaoFim?: string | null; pontosGps?: number; }

/** GET /api/passagens: o que o carro mandou em cada noite (operador da via, técnico, supervisor). */
@Injectable({ providedIn: 'root' })
export class Passagens {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/passagens`;

  ultimas(quantas = 30): Observable<PassagemDto[]> {
    return this.http.get<PassagemDto[]>(this.url, { params: consulta({ quantas }) });
  }
  buscar(id: number): Observable<PassagemDetalheDto> {
    return this.http.get<PassagemDetalheDto>(`${this.url}/${id}`);
  }
}
