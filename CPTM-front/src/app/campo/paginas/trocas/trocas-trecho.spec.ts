import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { GUARDADO } from '../../../nucleo/fila/guardado';
import { Fila } from '../../../nucleo/fila/fila';
import { Rede } from '../../../nucleo/fila/rede';
import { LoteTrocaDto, TrocaDto } from '../../entidades/campo';
import { TrocasTrecho } from './trocas-trecho';
import { LOTE_SEPARADO } from './trocar-lote.spec';

const troca = (id: number, status: TrocaDto['status'] = 'Separada') =>
  ({ id, status, codigoSaiu: `DM-E01-L08-00${id}`, idDormenteSaiu: id, km: 213.1, diasDesdeOPedido: 1 }) as TrocaDto;

describe('Trocas do trecho com a fila', () => {
  // Sem rede: a fila não manda, e a troca feita no celular fica esperando.
  const abrir = async (aba: 'trocar' | 'feitas', status: string, daApi: TrocaDto[], lotes: LoteTrocaDto[] = [], naFilaDoLote = false) => {
    localStorage.clear();
    entrarComo('OperadorVia');
    TestBed.configureTestingModule({
      imports: [TrocasTrecho],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), { provide: Rede, useValue: { online: signal(false) } }],
    });
    const naFila = { tipo: 'troca', alvo: 10, corpo: {}, rotulo: 'Troca da peça DM-E01-L08-0010', resumo: troca(10), id: 'x', idUsuario: 1, criadoEm: new Date().toISOString(), foto: 'x.jpg', tentativas: 0 };
    const loteNaFila = { ...naFila, tipo: 'lote', alvo: 3, id: 'y', rotulo: 'Lote LT-0003' };
    await TestBed.inject(GUARDADO).gravar('ybyra.fila', JSON.stringify({ itens: naFilaDoLote ? [naFila, loteNaFila] : [naFila], naoEnviados: [] }));
    await vi.waitFor(() => expect(TestBed.inject(Fila).itens()).toHaveLength(naFilaDoLote ? 2 : 1));
    const fixture = TestBed.createComponent(TrocasTrecho);
    fixture.componentRef.setInput('aba', aba);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne((r) => r.url.endsWith('/trocas') && r.params.get('status') === status).flush(daApi);
    http.match((r) => r.url.endsWith('/lotes-troca')).forEach((r) => r.flush(lotes));
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

  it('o lote separado aparece em "A trocar" (Victor, 2026-11-11)', async () => {
    const tela = await abrir('trocar', 'Separada', [], [LOTE_SEPARADO]);
    expect(cartoes(tela).find((c) => c.includes('LT-0003'))).toContain('2 peças');
  });

  it('o lote fechado no celular sai na hora de "A trocar"', async () => {
    const tela = await abrir('trocar', 'Separada', [], [LOTE_SEPARADO], true);
    expect(cartoes(tela).some((c) => c.includes('LT-0003'))).toBe(false);
  });

  it('e aparece em "Feitas", esperando a rede', async () => {
    const tela = await abrir('feitas', 'Feita', []);
    expect(cartoes(tela)).toHaveLength(1);
    expect(cartoes(tela)[0]).toContain('DM-E01-L08-0010');
    expect(cartoes(tela)[0]).toContain('Esperando a rede');
  });

  // ---------- o dormente que nunca teve tag (Victor, 2026-11-17) ----------

  it('a troca do dormente antigo, sem cadastro, aparece em "Feitas" como "Dormente antigo"', async () => {
    const antiga = { ...troca(20, 'Feita'), codigoSaiu: null, idDormenteSaiu: null, saiuNuncaTeveTag: true } as TrocaDto;
    const tela = await abrir('feitas', 'Feita', [antiga]);
    expect(cartoes(tela).find((c) => c.includes('Dormente antigo'))).toBeTruthy();
    expect(cartoes(tela).some((c) => c.includes('null'))).toBe(false);
  });

  it('"nunca teve tag" desmarca "não responde" e manda a troca sem ler a peça que sai', async () => {
    localStorage.clear();
    entrarComo('OperadorVia');
    const registrar = vi.fn(async () => ({ estado: 'enviado' as const, resposta: {} }));
    TestBed.configureTestingModule({
      imports: [TrocasTrecho],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
        { provide: Fila, useValue: { registrar, meus: signal([]), itens: signal([]), enviando: signal(false) } }],
    });
    const fixture = TestBed.createComponent(TrocasTrecho);
    fixture.detectChanges();
    type Tela = {
      marcarNaoResponde(v: boolean): void; marcarNuncaTeveTag(v: boolean): void; executar(t: TrocaDto): void;
      saiNaoResponde(): boolean; saiNuncaTeveTag(): boolean;
      tagEntra: { set(v: string): void }; equipe: { set(v: string): void }; foto: { set(v: File): void };
    };
    const t = fixture.componentInstance as unknown as Tela;

    t.marcarNaoResponde(true);
    t.marcarNuncaTeveTag(true);
    expect([t.saiNaoResponde(), t.saiNuncaTeveTag()]).toEqual([false, true]);
    t.tagEntra.set('E1');
    t.equipe.set('Turma de Via 2');
    t.foto.set(new File(['a'], 'a.jpg', { type: 'image/jpeg' }));
    t.executar(troca(10));
    await vi.waitFor(() => expect(registrar).toHaveBeenCalledTimes(1));
    const [registro] = registrar.mock.calls[0] as unknown as [{ corpo: { leituraSaiu: unknown; tagSaiuNaoResponde: boolean; saiuNuncaTeveTag: boolean } }];
    expect(registro.corpo).toMatchObject({ leituraSaiu: null, tagSaiuNaoResponde: false, saiuNuncaTeveTag: true });
  });
});
