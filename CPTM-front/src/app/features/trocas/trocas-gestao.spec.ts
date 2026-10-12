import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TrocaDto } from '../../models/campo';
import { entrarComo } from '../../testes/sessao-de-teste';
import { TrocasGestao } from './trocas-gestao';

const troca = { id: 5, status: 'Pedida', origem: 'Requisicao', idDormenteSaiu: 9, codigoSaiu: 'DM-9', tagSaiu: 'BR9', idDormenteEntrou: null, codigoEntrou: null, tagEntrou: null,
  idInspecao: null, idTrecho: 1, km: 213.1, motivo: 'Tag muda', observacao: null, idUsuarioPediu: 2, dataPedido: '2026-10-11T10:00:00', idUsuarioAprovou: null, dataAprovacao: null,
  motivoReprovacao: null, idUsuarioSeparou: null, dataSeparacao: null, idUsuarioFez: null, data: null, equipe: null, fotoArquivo: null, cancelamento: null, diasDesdeOPedido: 1, ultimaRecusa: null } as TrocaDto;

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
    http.match(() => true).forEach((r) => r.flush(r.request.url.endsWith('/trocas/5') ? troca : [troca]));
    await fixture.whenStable();
    const tela = fixture.nativeElement as HTMLElement;
    const botao = (texto: string) => [...tela.querySelectorAll('button')].find((b) => b.textContent?.includes(texto))!;
    botao('Reprovar').click();
    await fixture.whenStable();
    http.expectNone((r) => r.url.endsWith('/decisao'));
    expect(tela.querySelector('.aviso--erro')?.textContent).toContain('escreva o motivo');
    botao('Aprovar a troca').click();
    const req = http.expectOne('http://localhost:5169/api/trocas/5/decisao');
    expect(req.request.body).toEqual({ aprovar: true, motivo: null });
  });
});
