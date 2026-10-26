import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { GUARDADO } from '../../../nucleo/fila/guardado';
import { Fila } from '../../../nucleo/fila/fila';
import { Rede } from '../../../nucleo/fila/rede';
import { TrocaDto } from '../../entidades/campo';
import { TrocasTrecho } from './trocas-trecho';

const troca = (id: number, status: TrocaDto['status'] = 'Separada') =>
  ({ id, status, codigoSaiu: `DM-E01-L08-00${id}`, idDormenteSaiu: id, km: 213.1, diasDesdeOPedido: 1 }) as TrocaDto;

describe('Trocas do trecho com a fila', () => {
  // Sem rede: a fila não manda, e a troca feita no celular fica esperando.
  const abrir = async (aba: 'trocar' | 'feitas', status: string, daApi: TrocaDto[]) => {
    localStorage.clear();
    entrarComo('OperadorVia');
    TestBed.configureTestingModule({
      imports: [TrocasTrecho],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), { provide: Rede, useValue: { online: signal(false) } }],
    });
    const naFila = { tipo: 'troca', alvo: 10, corpo: {}, rotulo: 'Troca da peça DM-E01-L08-0010', resumo: troca(10), id: 'x', idUsuario: 1, criadoEm: new Date().toISOString(), foto: 'x.jpg', tentativas: 0 };
    await TestBed.inject(GUARDADO).gravar('ybyra.fila', JSON.stringify({ itens: [naFila], naoEnviados: [] }));
    await vi.waitFor(() => expect(TestBed.inject(Fila).itens()).toHaveLength(1));
    const fixture = TestBed.createComponent(TrocasTrecho);
    fixture.componentRef.setInput('aba', aba);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne((r) => r.url.endsWith('/trocas') && r.params.get('status') === status).flush(daApi);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  };
  const cartoes = (tela: HTMLElement) => [...tela.querySelectorAll('.cartao-alerta')].map((c) => c.textContent ?? '');

  it('a troca feita no celular sai na hora de "A trocar"', async () => {
    const tela = await abrir('trocar', 'Separada', [troca(10), troca(11)]);
    expect(cartoes(tela)).toHaveLength(1);
    expect(cartoes(tela)[0]).toContain('DM-E01-L08-0011');
  });

  it('e aparece em "Feitas", esperando a rede', async () => {
    const tela = await abrir('feitas', 'Feita', []);
    expect(cartoes(tela)).toHaveLength(1);
    expect(cartoes(tela)[0]).toContain('DM-E01-L08-0010');
    expect(cartoes(tela)[0]).toContain('Esperando a rede');
  });
});
