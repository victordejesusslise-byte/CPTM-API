import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LoteTrocaDto, PecaDoLoteDto } from '../../entidades/campo';
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
});
