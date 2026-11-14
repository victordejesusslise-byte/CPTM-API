import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { EmpresaDto } from '../../entidades/cadastro';
import { Cadastros, conferirSigla } from './cadastros';

const API = 'http://localhost:8045/api';
const EMPRESAS: EmpresaDto[] = [
  { id: 1, nome: 'Granoski', sigla: null, cnpj: null, papel: 'Tratadora', ativo: true, medidas: [] },
  { id: 2, nome: 'TG–Três Guri', sigla: 'TG', cnpj: null, papel: 'Fornecedor', ativo: true, medidas: [] },
];

describe('conferirSigla (a sigla da empresa: de 2 a 6 letras ou números, em maiúsculas)', () => {
  it('põe em maiúsculas, tira os espaços das pontas e deixa vazia como sem sigla', () => {
    expect(conferirSigla(' tg ')).toEqual({ sigla: 'TG' });
    expect(conferirSigla('ab12')).toEqual({ sigla: 'AB12' });
    expect(conferirSigla('   ')).toEqual({ sigla: null });
  });

  it('recusa 1 letra, mais de 6, espaço, traço e acento', () => {
    for (const ruim of ['T', 'TGTGTGT', 'T G', 'T-G', 'TÊ'])
      expect(conferirSigla(ruim)).toEqual({ erro: 'A sigla tem de 2 a 6 letras ou números, sem espaço nem traço (ex.: TG).' });
  });
});

describe('Empresas: o campo Sigla (Victor, 2026-11-14)', () => {
  const abrir = async () => {
    localStorage.clear();
    entrarComo('Supervisor');
    await TestBed.configureTestingModule({ imports: [Cadastros], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(Cadastros);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const responder = () => {
      http.match((r) => r.url === `${API}/empresas` && r.method === 'GET').forEach((r) => r.flush(EMPRESAS));
      http.match((r) => r.method === 'GET' && /\/(contratos|estacoes|trechos|parametros|catalogos\/especies)$/.test(r.url)).forEach((r) => r.flush([]));
    };
    /** As listas (rxResource) só pedem na detecção de mudanças: detecta, responde e repete, antes de esperar a tela parar. */
    const atualizar = async () => {
      for (let i = 0; i < 3; i++) {
        fixture.detectChanges();
        responder();
        await Promise.resolve();
      }
      await fixture.whenStable();
      fixture.detectChanges();
    };
    await atualizar();
    const tela = fixture.nativeElement as HTMLElement;
    const botao = (texto: string) => [...tela.querySelectorAll('button')].find((b) => b.textContent?.includes(texto))!;
    const campo = (rotulo: string) =>
      [...tela.querySelectorAll('mat-form-field')].find((c) => c.querySelector('mat-label')?.textContent?.includes(rotulo))?.querySelector('input') as HTMLInputElement;
    const escrever = async (input: HTMLInputElement, valor: string) => {
      input.value = valor;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
      await fixture.whenStable();
    };
    return { fixture, tela, http, botao, campo, escrever, atualizar };
  };

  it('a lista mostra a sigla de cada empresa', async () => {
    const { tela } = await abrir();
    expect([...tela.querySelectorAll('thead th')].map((th) => th.textContent?.trim())).toContain('Sigla');
    const siglas = [...tela.querySelectorAll<HTMLInputElement>('tbody input[aria-label^="Sigla de"]')].map((i) => [i.getAttribute('aria-label'), i.value]);
    expect(siglas).toEqual([['Sigla de Granoski', ''], ['Sigla de TG–Três Guri', 'TG']]);
  });

  it('cadastra a empresa com a sigla em maiúsculas; com a sigla fora do padrão, nem manda', async () => {
    const { tela, fixture, http, botao, campo, escrever } = await abrir();
    await escrever(campo('Nome'), 'Madeiras Ipê Forte');
    await escrever(campo('Sigla'), 'i-f');
    await escrever(campo('Largura'), '0.24');
    await escrever(campo('Altura'), '0.17');
    botao('Cadastrar empresa').click();
    fixture.detectChanges();
    expect(http.match((r) => r.url === `${API}/empresas` && r.method === 'POST')).toEqual([]);
    expect(tela.textContent).toContain('A sigla tem de 2 a 6 letras ou números, sem espaço nem traço (ex.: TG).');

    await escrever(campo('Sigla'), 'if');
    botao('Cadastrar empresa').click();
    const envio = http.expectOne((r) => r.url === `${API}/empresas` && r.method === 'POST');
    expect(envio.request.body).toMatchObject({ nome: 'Madeiras Ipê Forte', sigla: 'IF', papel: 'Fornecedor' });
  });

  it('muda a sigla na própria lista (a empresa que veio de antes, sem sigla, ganha uma)', async () => {
    const { tela, fixture, http, atualizar } = await abrir();
    const granoski = tela.querySelector<HTMLInputElement>('tbody input[aria-label="Sigla de Granoski"]')!;
    granoski.value = 'gr';
    granoski.dispatchEvent(new Event('input'));
    granoski.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    const envio = http.expectOne(`${API}/empresas/1/sigla`);
    expect(envio.request.method).toBe('PUT');
    expect(envio.request.body).toEqual({ sigla: 'GR' });
    envio.flush({ ...EMPRESAS[0], sigla: 'GR' });
    await atualizar(); // a lista recarrega
    expect(tela.textContent).toContain('Sigla de Granoski alterada para GR.');

    const tg = tela.querySelector<HTMLInputElement>('tbody input[aria-label="Sigla de TG–Três Guri"]')!;
    tg.value = 'T G';
    tg.dispatchEvent(new Event('input'));
    tg.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(http.match((r) => r.url.endsWith('/sigla'))).toEqual([]);
    expect(tela.textContent).toContain('A sigla tem de 2 a 6 letras ou números');
  });
});
