import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LoteTrocaDto } from '../../entidades/campo';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { LoteAberto } from './lote-aberto';

export const LOTE_PEDIDO = {
  id: 3, codigo: 'LT-0003', status: 'Pedido', idTrecho: 4, trecho: 'Brás – Tatuapé', classeExigida: 1, kmInicio: 23.1, kmFim: 23.4, idTamanho: 2, comprimentoM: 2.8,
  quantidadePedida: 12, quantidadeAprovada: null, motivo: 'Dormentes podres', observacao: null, idUsuarioPediu: 2, dataPedido: '2026-11-11T14:20:00',
  idUsuarioAprovou: null, dataAprovacao: null, motivoReprovacao: null, motivoMenos: null, noiteDoPlano: null, urgente: false, idUsuarioFez: null, data: null,
  equipe: null, cancelamento: null, separadas: [], devolvidas: [], trocas: [], pecasDaFaixa: [], ultimaRecusa: null,
  avulsasAbertas: [{ id: 9, codigo: 'DOR-000123', tag: 'BR9', kmProjeto: 23.15, idTamanho: 2, pilha: null }],
} as LoteTrocaDto;

describe('LoteAberto (quem aprova decide o lote inteiro)', () => {
  const abrir = async () => {
    localStorage.clear();
    entrarComo('Supervisor');
    await TestBed.configureTestingModule({ imports: [LoteAberto], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(LoteAberto);
    fixture.componentRef.setInput('lote', LOTE_PEDIDO);
    const http = TestBed.inject(HttpTestingController);
    // A prévia do laudo pede o documento: responde em rodadas, antes de esperar a tela ficar parada.
    for (let rodada = 0; rodada < 3; rodada++) {
      fixture.detectChanges();
      http.match((r) => r.url.includes('/laudos/')).forEach((r) => r.flush({ titulo: '', codigo: 'LD-LT0003-v1', subtitulo: '', geradoEm: '', secoes: [] }));
      await new Promise((fim) => setTimeout(fim));
    }
    await fixture.whenStable();
    const tela = fixture.nativeElement as HTMLElement;
    const botao = (texto: string) => [...tela.querySelectorAll('button')].find((b) => b.textContent?.includes(texto))!;
    return { fixture, http, tela, botao, aberto: fixture.componentInstance };
  };

  it('mostra o pedido avulso aberto no trecho', async () => {
    const { tela } = await abrir();
    expect(tela.textContent).toContain('DOR-000123');
  });

  it('aprovar menos que o pedido sem o porquê mostra o erro e não chama a API', async () => {
    const { fixture, http, tela, botao, aberto } = await abrir();
    aberto.quantidade.set(10);
    fixture.detectChanges();
    botao('Aprovar o lote').click();
    fixture.detectChanges();
    http.expectNone((r) => r.url.endsWith('/decisao'));
    expect(tela.querySelector('.aviso--erro')?.textContent).toContain('porquê');

    aberto.motivo.set('2 ainda aguentam');
    fixture.detectChanges();
    expect(botao('Aprovar o lote').textContent).toContain('10 peças');
    botao('Aprovar o lote').click();
    const req = http.expectOne('http://localhost:8045/api/lotes-troca/3/decisao');
    expect(req.request.body).toEqual({ aprovar: true, quantidade: 10, motivo: '2 ainda aguentam', urgente: false });
  });

  it('reprovar exige motivo', async () => {
    const { fixture, http, tela, botao } = await abrir();
    botao('Reprovar').click();
    fixture.detectChanges();
    http.expectNone((r) => r.url.endsWith('/decisao'));
    expect(tela.querySelector('.aviso--erro')?.textContent).toContain('motivo');
  });
});
