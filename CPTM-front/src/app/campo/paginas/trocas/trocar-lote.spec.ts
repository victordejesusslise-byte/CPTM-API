import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LoteTrocaDto, ObrigatoriaDto, PecaDoLoteDto } from '../../entidades/campo';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { Fila } from '../../../nucleo/fila/fila';
import { LOTE_PEDIDO } from './lote-aberto.spec';
import { TrocarLote, conferirPar } from './trocar-lote';

const peca = (id: number, tag: string, km: number | null = null): PecaDoLoteDto => ({ id, codigo: `DOR-${id}`, tag, kmProjeto: km, idTamanho: 2, pilha: null });
export const LOTE_SEPARADO = {
  ...LOTE_PEDIDO, status: 'Separado', quantidadeAprovada: 2, avulsasAbertas: [],
  separadas: [peca(11, 'E1'), peca(12, 'E2'), peca(13, 'E3')],
  pecasDaFaixa: [peca(1, 'S1', 23.2), peca(2, 'S2', 23.21), peca(3, 'S3', 23.22)],
} as LoteTrocaDto;

describe('TrocarLote (a via troca dormente por dormente e fecha o lote)', () => {
  it('confere cada par: fora da faixa, não separada, repetida e além da quantidade', () => {
    const um = conferirPar(LOTE_SEPARADO, [], { tag: 'S1' }, 'E1');
    expect(um).toEqual({ saiu: LOTE_SEPARADO.pecasDaFaixa[0], entrou: LOTE_SEPARADO.separadas[0] });
    expect(conferirPar(LOTE_SEPARADO, [], { tag: 'X9' }, 'E1')).toContain('não está na via dentro da faixa');
    expect(conferirPar(LOTE_SEPARADO, [], { tag: 'S1' }, 'E9')).toContain('não é uma das separadas');
    const feito = { saiu: LOTE_SEPARADO.pecasDaFaixa[0], entrou: LOTE_SEPARADO.separadas[0] };
    expect(conferirPar(LOTE_SEPARADO, [feito], { tag: 'S1' }, 'E2')).toContain('DOR-1 já saiu');
    expect(conferirPar(LOTE_SEPARADO, [feito], { tag: 'S2' }, 'E1')).toContain('DOR-11 já entrou');
    const dois = [feito, { saiu: LOTE_SEPARADO.pecasDaFaixa[1], entrou: LOTE_SEPARADO.separadas[1] }];
    expect(conferirPar(LOTE_SEPARADO, dois, { tag: 'S3' }, 'E3')).toContain('aprovou 2');
    // Tag que não responde: a peça escolhida pelo km, na lista da faixa.
    expect(conferirPar(LOTE_SEPARADO, [], { idDormente: 3 }, 'E3')).toEqual({ saiu: LOTE_SEPARADO.pecasDaFaixa[2], entrou: LOTE_SEPARADO.separadas[2] });
  });

  it('guarda cada dormente e Confirmar o lote manda os pares e uma foto por par para a fila', async () => {
    localStorage.clear();
    entrarComo('OperadorVia');
    const registrar = vi.fn(async () => ({ estado: 'enviado' as const, resposta: {} }));
    await TestBed.configureTestingModule({
      imports: [TrocarLote],
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Fila, useValue: { registrar, meus: signal([]), itens: signal([]), enviando: signal(false) } }],
    }).compileComponents();
    const fixture = TestBed.createComponent(TrocarLote);
    fixture.componentRef.setInput('lote', LOTE_SEPARADO);
    fixture.detectChanges();
    await fixture.whenStable();
    const t = fixture.componentInstance;

    t.tagSai.set('S1');
    t.tagEntra.set('E1');
    await t.guardar(); // sem foto: não guarda
    expect(t.rascunho()).toHaveLength(0);
    t.foto.set(new File(['a'], 'a.jpg', { type: 'image/jpeg' }));
    await t.guardar();
    t.tagSai.set('S2');
    t.tagEntra.set('E2');
    t.foto.set(new File(['b'], 'b.jpg', { type: 'image/jpeg' }));
    await t.guardar();
    expect(t.rascunho().map((p) => [p.saiu.codigo, p.entrou.codigo])).toEqual([['DOR-1', 'DOR-11'], ['DOR-2', 'DOR-12']]);

    await t.confirmar(); // sem equipe: não manda
    expect(registrar).not.toHaveBeenCalled();
    t.equipe.set('Turma de Via 3');
    await t.confirmar();
    expect(registrar).toHaveBeenCalledTimes(1);
    const [registro, fotos] = registrar.mock.calls[0] as unknown as [{ tipo: string; alvo: number; corpo: { equipe: string; pares: { leituraSaiu: { tag: string }; leituraEntrou: { tag: string }; tagSaiuNaoResponde: boolean }[] } }, Blob[]];
    expect([registro.tipo, registro.alvo, registro.corpo.equipe]).toEqual(['lote', 3, 'Turma de Via 3']);
    expect(registro.corpo.pares.map((p) => [p.leituraSaiu.tag, p.leituraEntrou.tag, p.tagSaiuNaoResponde])).toEqual([['S1', 'E1', false], ['S2', 'E2', false]]);
    expect(fotos).toHaveLength(2);
    expect(t.rascunho()).toEqual([]); // enviado: o rascunho sai do celular
  });

  // ---------- as trocas avulsas já aprovadas na faixa são obrigatórias (Victor, 2026-11-12) ----------

  const obrigatoria: ObrigatoriaDto = { idTroca: 7, situacao: 'Aberta', peca: peca(3, 'S3', 23.22), separada: peca(20, 'EA'), motivo: null };
  /** Aprovou 2 e tem 1 obrigatória (a peça 3, com a peça 20 já separada no avulso): fecha com até 3. */
  const COM_OBRIGATORIA = { ...LOTE_SEPARADO, pecasDaFaixa: [...LOTE_SEPARADO.pecasDaFaixa, peca(4, 'S4', 23.23)], obrigatorias: [obrigatoria] } as LoteTrocaDto;

  it('a obrigatória soma à quantidade, a peça do avulso só entra no lugar dele e o último lugar é dela', () => {
    expect(conferirPar(COM_OBRIGATORIA, [], { tag: 'S1' }, 'EA')).toContain('só entra no lugar');
    expect(conferirPar(COM_OBRIGATORIA, [], { tag: 'S3' }, 'EA')).toEqual({ saiu: COM_OBRIGATORIA.pecasDaFaixa[2], entrou: obrigatoria.separada });
    const dois = [{ saiu: COM_OBRIGATORIA.pecasDaFaixa[0], entrou: COM_OBRIGATORIA.separadas[0] }, { saiu: COM_OBRIGATORIA.pecasDaFaixa[1], entrou: COM_OBRIGATORIA.separadas[1] }];
    expect(conferirPar(COM_OBRIGATORIA, dois, { tag: 'S4' }, 'E3')).toContain('DOR-3'); // o lugar que sobra é da obrigatória
    expect(conferirPar(COM_OBRIGATORIA, dois, { tag: 'S4' }, 'E3', [7])).toEqual({ saiu: COM_OBRIGATORIA.pecasDaFaixa[3], entrou: COM_OBRIGATORIA.separadas[2] }); // não deu: o lugar fica livre
    expect(conferirPar(COM_OBRIGATORIA, dois, { tag: 'S3' }, 'EA')).toEqual({ saiu: COM_OBRIGATORIA.pecasDaFaixa[2], entrou: obrigatoria.separada }); // a 3ª troca: 2 + 1
  });

  it('a peça do avulso que entrou não conta como sobra do lote', async () => {
    localStorage.clear();
    entrarComo('OperadorVia');
    await TestBed.configureTestingModule({
      imports: [TrocarLote],
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Fila, useValue: { registrar: vi.fn(), meus: signal([]), itens: signal([]), enviando: signal(false) } }],
    }).compileComponents();
    const fixture = TestBed.createComponent(TrocarLote);
    fixture.componentRef.setInput('lote', COM_OBRIGATORIA);
    fixture.detectChanges();
    await fixture.whenStable();
    const t = fixture.componentInstance;
    t.tagSai.set('S3');
    t.tagEntra.set('EA');
    t.foto.set(new File(['a'], 'a.jpg', { type: 'image/jpeg' }));
    await t.guardar();
    expect(t.rascunho()).toHaveLength(1);
    expect(t['sobram']()).toBe(3); // as 3 separadas do lote continuam, e voltam ao armazém se não forem usadas
  });

  it('sem trocar nem dizer que não deu a obrigatória, o lote não fecha; o não deu vai com motivo e foto depois das fotos dos pares', async () => {
    localStorage.clear();
    entrarComo('OperadorVia');
    const registrar = vi.fn(async () => ({ estado: 'enviado' as const, resposta: {} }));
    await TestBed.configureTestingModule({
      imports: [TrocarLote],
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Fila, useValue: { registrar, meus: signal([]), itens: signal([]), enviando: signal(false) } }],
    }).compileComponents();
    const fixture = TestBed.createComponent(TrocarLote);
    fixture.componentRef.setInput('lote', COM_OBRIGATORIA);
    fixture.detectChanges();
    await fixture.whenStable();
    const t = fixture.componentInstance;
    expect(fixture.nativeElement.textContent).toContain('DOR-3');

    t.tagSai.set('S1');
    t.tagEntra.set('E1');
    t.foto.set(new File(['a'], 'a.jpg', { type: 'image/jpeg' }));
    await t.guardar();
    t.equipe.set('Turma de Via 3');
    await t.confirmar();
    expect(registrar).not.toHaveBeenCalled(); // faltou a obrigatória
    expect(fixture.componentInstance['erro']()).toContain('DOR-3');

    t.abrirNaoDeu(obrigatoria);
    await t.guardarNaoDeu(); // sem motivo nem foto: não guarda
    expect(t.naoDeu()).toHaveLength(0);
    t.motivo.set('Preso no AMV');
    t.foto.set(new File(['n'], 'n.jpg', { type: 'image/jpeg' }));
    await t.guardarNaoDeu();
    expect(t.naoDeu().map((n) => [n.idTroca, n.motivo])).toEqual([[7, 'Preso no AMV']]);

    await t.confirmar();
    expect(registrar).toHaveBeenCalledTimes(1);
    const [registro, fotos] = registrar.mock.calls[0] as unknown as [{ corpo: { pares: unknown[]; naoDeu: { idTroca: number; motivo: string }[] } }, Blob[]];
    expect(registro.corpo.pares).toHaveLength(1);
    expect(registro.corpo.naoDeu.map((n) => [n.idTroca, n.motivo])).toEqual([[7, 'Preso no AMV']]);
    expect(fotos).toHaveLength(2); // a do par e a do não deu, nessa ordem
    expect(t.naoDeu()).toEqual([]);
  });
});
