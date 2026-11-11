import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LoteTrocaDto } from '../../entidades/campo';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { LOTE_PEDIDO } from './lote-aberto.spec';
import { SepararLote } from './separar-lote';

describe('SepararLote (o pátio separa as peças do lote)', () => {
  const abrir = async (lote: LoteTrocaDto) => {
    localStorage.clear();
    entrarComo('OperadorPatio');
    await TestBed.configureTestingModule({ imports: [SepararLote], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(SepararLote);
    fixture.componentRef.setInput('lote', lote);
    fixture.detectChanges();
    return { fixture, http: TestBed.inject(HttpTestingController), separar: fixture.componentInstance };
  };
  const compativel = { peca: { id: 7, codigo: 'DM-7', tag: 'E7', especie: 'Angelim', comprimentoM: 2.8, classe: 1, pilha: 'A01' }, distanciaKm: 1, idadeDias: 30, sugerida: true };

  it('separa a peça escolhida, lendo a tag dela', async () => {
    const { fixture, http, separar } = await abrir({ ...LOTE_PEDIDO, status: 'Aprovado', quantidadeAprovada: 2 } as LoteTrocaDto);
    http.expectOne('http://localhost:8045/api/lotes-troca/3/compativeis').flush([compativel]);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Faltam');
    separar.escolhida.set(compativel as never);
    separar.tag.set('E7');
    separar.separar();
    const req = http.expectOne('http://localhost:8045/api/lotes-troca/3/separacao');
    expect(req.request.body).toMatchObject({ idDormente: 7, leitura: { tag: 'E7' } });
  });

  it('com as aprovadas já separadas, não oferece mais peças', async () => {
    const pronta = { ...LOTE_PEDIDO, status: 'Separado', quantidadeAprovada: 1, separadas: [{ id: 7, codigo: 'DM-7', tag: 'E7', kmProjeto: null, idTamanho: 2, pilha: null }] } as LoteTrocaDto;
    const { fixture, http } = await abrir(pronta);
    http.expectNone((r) => r.url.endsWith('/compativeis'));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('todas as peças aprovadas estão separadas');
  });
});
