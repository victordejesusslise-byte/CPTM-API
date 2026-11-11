import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { LoteTrocaDto } from '../../entidades/campo';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { LOTE_PEDIDO } from './lote-aberto.spec';
import { SepararPeca } from './separar-peca';

describe('SepararPeca', () => {
  const abrir = async (lotes: LoteTrocaDto[]) => {
    localStorage.clear();
    entrarComo('OperadorPatio');
    await TestBed.configureTestingModule({ imports: [SepararPeca], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(SepararPeca);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const pedidos = http.match((r) => r.url === 'http://localhost:8045/api/trocas');
    const doLote = http.match((r) => r.url === 'http://localhost:8045/api/lotes-troca');
    pedidos.forEach((r) => r.flush([]));
    doLote.forEach((r) => r.flush(lotes.filter((l) => l.status === r.request.params.get('status'))));
    await fixture.whenStable();
    fixture.detectChanges();
    return { tela: fixture.nativeElement as HTMLElement, pedidos, doLote };
  };

  it('busca as trocas aprovadas e as que esperam peça', async () => {
    const { tela, pedidos } = await abrir([]);
    expect(pedidos.map((r) => r.request.params.get('status')).sort()).toEqual(['AguardandoPeca', 'Aprovada']);
    expect(tela.textContent).toContain('Nenhuma troca aqui.');
  });

  it('o lote aprovado aparece para separar, e o já completo não (Victor, 2026-11-11)', async () => {
    const aprovado = { ...LOTE_PEDIDO, status: 'Aprovado', quantidadeAprovada: 10 } as LoteTrocaDto;
    const completo = { ...LOTE_PEDIDO, id: 4, codigo: 'LT-0004', status: 'Separado', quantidadeAprovada: 1, separadas: [{ id: 7, codigo: 'DM-7', tag: 'E7', kmProjeto: null, idTamanho: 2, pilha: null }] } as LoteTrocaDto;
    const { tela, doLote } = await abrir([aprovado, completo]);
    expect(doLote.map((r) => r.request.params.get('status')).sort()).toEqual(['AguardandoPeca', 'Aprovado', 'Separado']);
    expect(tela.textContent).toContain('LT-0003');
    expect(tela.textContent).toContain('separar 10');
    expect(tela.textContent).not.toContain('LT-0004');
  });
});
