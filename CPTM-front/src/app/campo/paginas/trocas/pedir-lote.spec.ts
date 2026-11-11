import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { PedirLote } from './pedir-lote';

describe('PedirLote (o técnico pede um lote de troca)', () => {
  const abrir = async () => {
    localStorage.clear();
    entrarComo('Tecnico');
    await TestBed.configureTestingModule({ imports: [PedirLote], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(PedirLote);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.match((r) => r.url.endsWith('/trechos')).forEach((r) => r.flush([{ id: 4, nome: 'Brás – Tatuapé', linha: '11', kmInicio: 23, kmFim: 25, pecas: 9, ruimOuCritico: 2 }]));
    http.match((r) => r.url.endsWith('/tamanhos')).forEach((r) => r.flush([{ id: 2, comprimentoM: 2.8 }]));
    fixture.detectChanges();
    await fixture.whenStable();
    const tela = fixture.nativeElement as HTMLElement;
    const botao = (texto: string) => [...tela.querySelectorAll('button')].find((b) => b.textContent?.includes(texto))!;
    return { fixture, http, tela, botao, pedir: fixture.componentInstance };
  };

  it('sem motivo não envia e diz o que falta', async () => {
    const { fixture, http, tela, botao, pedir } = await abrir();
    pedir.escolherTrecho(4);
    pedir.quantidade.set(12);
    pedir.idTamanho.set(2);
    botao('Pedir o lote').click();
    fixture.detectChanges();
    http.expectNone((r) => r.method === 'POST');
    expect(tela.querySelector('.aviso--erro')?.textContent).toContain('motivo');
  });

  it('manda o trecho, a faixa de km, a quantidade e o comprimento, sem escolher peças', async () => {
    const { http, botao, pedir } = await abrir();
    let pedido: unknown = null;
    pedir.pedido.subscribe((l) => (pedido = l));
    pedir.escolherTrecho(4);
    expect([pedir.kmInicio(), pedir.kmFim()]).toEqual([23, 25]); // começa com o trecho inteiro
    pedir.kmInicio.set(23.1);
    pedir.kmFim.set(23.4);
    pedir.quantidade.set(12);
    pedir.idTamanho.set(2);
    pedir.motivo.set('  Dormentes podres  ');
    botao('Pedir o lote').click();
    const req = http.expectOne((r) => r.method === 'POST' && r.url.endsWith('/lotes-troca'));
    expect(req.request.body).toEqual({ idTrecho: 4, kmInicio: 23.1, kmFim: 23.4, quantidade: 12, idTamanho: 2, motivo: 'Dormentes podres', observacao: null });
    req.flush({ id: 3, codigo: 'LT-0003' });
    expect(pedido).toEqual({ id: 3, codigo: 'LT-0003' });
  });
});
