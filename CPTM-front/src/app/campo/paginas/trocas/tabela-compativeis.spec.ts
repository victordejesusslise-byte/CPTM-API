import { TestBed } from '@angular/core/testing';
import { CompativelDto } from '../../entidades/campo';
import { TabelaCompativeis } from './tabela-compativeis';

// A tabela das peças compatíveis (Victor, 2026-11-17): uma linha por peça, toque escolhe, a sugerida tem a etiqueta.
describe('TabelaCompativeis (as peças que servem, na tabela do sistema)', () => {
  const peca = (id: number, codigo: string, sugerida: boolean) => ({
    peca: { id, codigo, tag: `E${id}`, especie: 'Ocotea neesiana', comprimentoM: 2.8, classe: 1, pilha: 'A01' },
    distanciaKm: 1, idadeDias: 35, sugerida,
  }) as unknown as CompativelDto;
  const lista = [peca(8, 'DM-E01-L01-0008', true), peca(9, 'DM-E01-L01-0009', false)];

  const abrir = async () => {
    await TestBed.configureTestingModule({ imports: [TabelaCompativeis] }).compileComponents();
    const fixture = TestBed.createComponent(TabelaCompativeis);
    fixture.componentRef.setInput('lista', lista);
    fixture.detectChanges();
    const linhas = () => [...(fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr')] as HTMLTableRowElement[];
    return { fixture, linhas };
  };

  it('mostra uma linha por peça, com espécie e classe, pilha e dias no armazém', async () => {
    const { linhas } = await abrir();
    expect(linhas().length).toBe(2);
    expect(linhas()[0].textContent).toContain('DM-E01-L01-0008');
    expect(linhas()[0].textContent).toContain('Ocotea neesiana · 1ª');
    expect(linhas()[0].textContent).toContain('A01 · 35 dias no armazém');
  });

  it('só a sugerida tem a etiqueta "sugerida"', async () => {
    const { linhas } = await abrir();
    expect(linhas()[0].textContent).toContain('sugerida');
    expect(linhas()[1].textContent).not.toContain('sugerida');
  });

  it('o toque na linha escolhe a peça e marca a linha', async () => {
    const { fixture, linhas } = await abrir();
    linhas()[1].click();
    fixture.detectChanges();
    expect(fixture.componentInstance.escolhida()?.peca.id).toBe(9);
    expect(linhas()[1].classList).toContain('selecionada');
    expect(linhas()[0].classList).not.toContain('selecionada');
  });
});
