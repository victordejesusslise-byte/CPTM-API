import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { legivel } from '../../utilitarios/legivel';
import { CartoesNoCelular } from './cartoes';

@Component({
  imports: [CartoesNoCelular],
  template: `<table class="simples">
    <thead><tr><th>Peça</th><th>Tag</th><th>Situação</th></tr></thead>
    <tbody><tr><td>DM-E01-L09-0067</td><td>BR0000000037</td><td>Aguardando verificação</td></tr></tbody>
  </table>`,
})
class Tela {}

describe('Tabelas em cartões no celular', () => {
  it('cada célula ganha o nome da coluna, e a situação vira o destaque do cartão', async () => {
    const fixture = TestBed.createComponent(Tela);
    await fixture.whenStable();
    const tabela = (fixture.nativeElement as HTMLElement).querySelector('table')!;
    expect(tabela.classList).toContain('cartoes');
    const [peca, tag, situacao] = [...tabela.querySelectorAll('td')];
    expect(peca.getAttribute('data-titulo')).toBe('Peça');
    expect(tag.getAttribute('data-titulo')).toBe('Tag');
    expect(situacao.classList).toContain('destaque');
    expect(peca.classList).not.toContain('destaque');
  });

  it('os nomes técnicos da API viram português; o resto fica como está', () => {
    expect(legivel('AguardandoVerificacao')).toBe('Aguardando verificação');
    expect(legivel('NaVia')).toBe('Na via');
    expect(legivel('Saida')).toBe('Saída');
    expect(legivel('OperadorPatio')).toBe('Operador do pátio');
    expect(legivel('Aprovada')).toBe('Aprovada');
    expect(legivel('DM-E01-L09-0067')).toBe('DM-E01-L09-0067');
    expect(legivel('BR0000000037')).toBe('BR0000000037');
    expect(legivel('Ana Souza')).toBe('Ana Souza');
    expect(legivel(12)).toBe(12);
  });
});
