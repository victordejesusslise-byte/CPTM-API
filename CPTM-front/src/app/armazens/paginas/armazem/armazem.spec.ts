import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ArmazemDto } from '../../../catalogos/entidades/catalogo';
import { LoteParaGuardarDto } from '../../../pecas/entidades/peca';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { Armazem } from './armazem';

// Guardar na pilha o lote inteiro (Victor, 2026-11-17): a tag lida acha o lote, separado por tamanho e classe, e cada grupo vai para a pilha dele.
describe('Armazem · guardar o lote inteiro', () => {
  const armazens: ArmazemDto[] = [{
    id: 3, nome: 'Santo Amaro', linha: '9', km: null, pecas: 10, pilhas: [
      { id: 17, codigo: 'A01', setor: 'A', comprimentoM: 2.8, classe: 1, pecas: 40 },
      { id: 21, codigo: 'C01', setor: 'C', comprimentoM: 4, classe: 1, pecas: 6 },
    ],
  }];
  const lote: LoteParaGuardarDto = {
    idLote: 4, codigoLote: 'L-PED-LOTE', idDormenteLido: 8, codigoLido: 'DM-E01-L04-0008', idArmazem: 3, pecas: 9,
    grupos: [
      { idTamanho: 1, comprimentoM: 2.8, classe: 1, aGuardar: 5, jaNaPilha: 2, naoAprovadas: 0 },
      { idTamanho: 3, comprimentoM: 4, classe: 1, aGuardar: 1, jaNaPilha: 0, naoAprovadas: 1 },
    ],
  };
  const pecaLida = { id: 8, codigo: 'DM-E01-L04-0008', idPilha: null, entradaArmazemEm: null, idArmazem: 3 };

  const abrir = async () => {
    localStorage.clear();
    entrarComo('OperadorPatio');
    await TestBed.configureTestingModule({ imports: [Armazem], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(Armazem);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    // Os catálogos e o estoque da tela respondem antes; o teste olha só o lote.
    http.match((r) => r.url.endsWith('/armazens')).forEach((r) => r.flush(armazens));
    http.match((r) => r.url.endsWith('/tamanhos')).forEach((r) => r.flush([]));
    http.match((r) => r.url.endsWith('/estoque')).forEach((r) => r.flush([]));
    await fixture.whenStable();
    return { fixture, http, tela: fixture.componentInstance as unknown as { tag: { set(v: string): void }; buscar(): void; guardarLote(): void } };
  };

  const lerTag = async (f: Awaited<ReturnType<typeof abrir>>) => {
    f.tela.tag.set('BR0000000008');
    f.tela.buscar();
    f.http.expectOne((r) => r.url.endsWith('/tags/BR0000000008')).flush(pecaLida);
    f.http.expectOne((r) => r.url.endsWith('/dormentes/lote-da-tag/BR0000000008')).flush(lote);
    f.fixture.detectChanges();
    await f.fixture.whenStable();
    f.fixture.detectChanges();
  };

  it('mostra o lote da tag lida, um grupo por tamanho e classe', async () => {
    const f = await abrir();
    await lerTag(f);
    const texto = (f.fixture.nativeElement as HTMLElement).textContent!;
    expect(texto).toContain('L-PED-LOTE');
    expect(texto).toContain('2,80 m · 1ª');
    expect(texto).toContain('5 a guardar · 2 já na pilha');
    expect(texto).toContain('1 a guardar · 1 não aprovada');
    expect(texto).toContain('Guardar as 6 peças nas pilhas');
  });

  it('guarda o lote com a pilha sugerida de cada grupo, no armazém do lote', async () => {
    const f = await abrir();
    await lerTag(f);
    f.tela.guardarLote();
    const req = f.http.expectOne((r) => r.url.endsWith('/dormentes/lote/armazem'));
    expect(req.request.body).toMatchObject({
      idArmazem: 3, leitura: { tag: 'BR0000000008' },
      grupos: [{ idTamanho: 1, classe: 1, idPilha: 17 }, { idTamanho: 3, classe: 1, idPilha: 21 }],
    });
  });
});
