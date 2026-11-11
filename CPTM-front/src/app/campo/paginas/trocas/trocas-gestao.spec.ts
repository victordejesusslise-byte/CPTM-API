import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TrocaDto } from '../../entidades/campo';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { TrocasGestao } from './trocas-gestao';
import { LOTE_PEDIDO } from './lote-aberto.spec';

const troca = { id: 5, status: 'Pedida', origem: 'Requisicao', idDormenteSaiu: 9, codigoSaiu: 'DM-9', tagSaiu: 'BR9', idDormenteEntrou: null, codigoEntrou: null, tagEntrou: null,
  idInspecao: null, idTrecho: 1, km: 213.1, motivo: 'Tag muda', observacao: null, idUsuarioPediu: 2, dataPedido: '2026-10-11T10:00:00', idUsuarioAprovou: null, dataAprovacao: null,
  motivoReprovacao: null, idUsuarioSeparou: null, dataSeparacao: null, idUsuarioFez: null, data: null, equipe: null, fotoArquivo: null, cancelamento: null, diasDesdeOPedido: 1, ultimaRecusa: null } as TrocaDto;

const documento = { titulo: 'Laudo da troca', codigo: 'LD-0005-v1', subtitulo: 'DM-9 · km 213,100', geradoEm: '2026-10-18T10:00:00',
  secoes: [{ titulo: 'Passos', campos: [{ rotulo: 'Situação', valor: 'Pedida' }], tabela: { colunas: ['Passo', 'Quem'], linhas: [['Pedida', 'Técnico']] } }] };

/** Responde a tudo o que a tela pediu, em rodadas: a prévia do laudo só pede o documento depois que a troca chega. */
const responder = async (fixture: ComponentFixture<TrocasGestao>, http: HttpTestingController, lotes: unknown[] = []) => {
  for (let rodada = 0; rodada < 4; rodada++) {
    http.match(() => true).forEach((r) => r.flush(
      r.request.url.endsWith('/documento') ? documento
        : r.request.url.endsWith('/lotes-troca/3') ? LOTE_PEDIDO
        : r.request.url.endsWith('/lotes-troca') ? lotes
        : r.request.url.endsWith('/trocas/5') ? troca : [troca]));
    fixture.detectChanges();
    await new Promise((fim) => setTimeout(fim));
  }
  await fixture.whenStable();
};

describe('TrocasGestao', () => {
  it('o supervisor não reprova sem motivo e aprova com um toque', async () => {
    localStorage.clear();
    entrarComo('Supervisor');
    await TestBed.configureTestingModule({ imports: [TrocasGestao], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(TrocasGestao);
    fixture.componentRef.setInput('aba', 'aprovar');
    fixture.componentRef.setInput('aberta', '5');
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    await responder(fixture, http);
    const tela = fixture.nativeElement as HTMLElement;
    const botao = (texto: string) => [...tela.querySelectorAll('button')].find((b) => b.textContent?.includes(texto))!;
    botao('Reprovar').click();
    await fixture.whenStable();
    http.expectNone((r) => r.url.endsWith('/decisao'));
    expect(tela.querySelector('.aviso--erro')?.textContent).toContain('escreva o motivo');
    botao('Aprovar a troca').click();
    const req = http.expectOne('http://localhost:8045/api/trocas/5/decisao');
    expect(req.request.body).toEqual({ aprovar: true, motivo: null });
  });

  it('o técnico também aprova (Victor, 2026-10-31), até o pedido que ele fez', async () => {
    localStorage.clear();
    entrarComo('Tecnico');
    await TestBed.configureTestingModule({ imports: [TrocasGestao], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(TrocasGestao);
    fixture.componentRef.setInput('aba', 'aprovar');
    fixture.componentRef.setInput('aberta', '5');
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    await responder(fixture, http);
    const tela = fixture.nativeElement as HTMLElement;
    [...tela.querySelectorAll('button')].find((b) => b.textContent?.includes('Aprovar a troca'))!.click();
    const req = http.expectOne('http://localhost:8045/api/trocas/5/decisao');
    expect(req.request.body).toEqual({ aprovar: true, motivo: null });
  });

  it('mostra o laudo da troca em andamento sem baixar; baixar só no laudo aberto', async () => {
    localStorage.clear();
    entrarComo('Tecnico');
    await TestBed.configureTestingModule({ imports: [TrocasGestao], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(TrocasGestao);
    fixture.componentRef.setInput('aba', 'andamento');
    fixture.componentRef.setInput('aberta', '5');
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    await responder(fixture, http);
    const tela = fixture.nativeElement as HTMLElement;

    expect(tela.querySelector('.folha-previa')?.textContent).toContain('LD-0005-v1');
    const foraDaJanela = [...tela.querySelectorAll('button')].filter((b) => !b.closest('dialog'));
    expect(foraDaJanela.some((b) => b.textContent?.includes('Baixar'))).toBe(false);
    expect(foraDaJanela.some((b) => b.textContent?.includes('Liberação'))).toBe(false); // pedida: a liberação ainda não sai
    expect(tela.querySelector('dialog')?.textContent).toContain('Baixar PDF');
  });

  it('o lote de troca pedido aparece em Para aprovar e abre para decidir (Victor, 2026-11-11)', async () => {
    localStorage.clear();
    entrarComo('Supervisor');
    await TestBed.configureTestingModule({ imports: [TrocasGestao], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(TrocasGestao);
    fixture.componentRef.setInput('aba', 'aprovar');
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    await responder(fixture, http, [LOTE_PEDIDO]);
    const tela = fixture.nativeElement as HTMLElement;
    const cartao = [...tela.querySelectorAll('.cartao-alerta')].find((c) => c.textContent?.includes('LT-0003'))!;
    expect(cartao.textContent).toContain('12 peças');
    expect(tela.textContent).toContain('2 · toque para abrir'); // a troca e o lote

    fixture.componentRef.setInput('lote', '3');
    fixture.detectChanges();
    await responder(fixture, http, [LOTE_PEDIDO]);
    expect(tela.querySelector('app-lote-aberto')?.textContent).toContain('Aprovar o lote · 12 peças');
  });

  it('o técnico pede um lote de troca em Trocas em andamento', async () => {
    localStorage.clear();
    entrarComo('Tecnico');
    await TestBed.configureTestingModule({ imports: [TrocasGestao], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(TrocasGestao);
    fixture.componentRef.setInput('aba', 'andamento');
    fixture.componentRef.setInput('novo', 'lote');
    fixture.detectChanges();
    await responder(fixture, TestBed.inject(HttpTestingController));
    expect((fixture.nativeElement as HTMLElement).querySelector('app-pedir-lote')?.textContent).toContain('As peças não são escolhidas agora');
  });
});
