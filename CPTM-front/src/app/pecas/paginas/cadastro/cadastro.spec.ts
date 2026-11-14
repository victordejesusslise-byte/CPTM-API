import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { NO_APP } from '../../../nucleo/ao-vivo/trabalho-da-noite';
import { ChegadaDto, FeixeDto, PedidoDto } from '../../../pedidos/entidades/pedido';
import { Cadastro } from './cadastro';

const API = 'http://localhost:8045/api';
const FEIXE = {
  id: 7, numero: '01', quantidade: 10, cadastradas: 2, idEspecie: 1, especie: 'Ocotea neesiana', classe: 1, idTamanho: 1, comprimentoM: 2.8,
  tratamento: 'CCA', idEmpresaTratamento: null, idEmpresaInspecao: null, anoInspecao: null,
} as FeixeDto;
const CHEGADA = { id: 5, idPedido: 1, quantidadeRecebida: 10, lote: 'L-PD-1', feixes: [FEIXE] } as unknown as ChegadaDto;
const PEDIDO = { id: 1, codigo: 'PD-20261017-195108', empresa: 'TG–Três Guri', pecasRecebidas: 10 } as PedidoDto;

describe('Cadastro da peça: a tag do fornecedor vem montada pelo pedido (Victor, 2026-11-14)', () => {
  const abrir = async () => {
    localStorage.clear();
    entrarComo('OperadorPatio');
    await TestBed.configureTestingModule({
      imports: [Cadastro],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), { provide: NO_APP, useValue: false }],
    }).compileComponents();
    const fixture = TestBed.createComponent(Cadastro);
    fixture.componentRef.setInput('pedido', '1');
    fixture.componentRef.setInput('chegada', '5');
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const tela = fixture.nativeElement as HTMLElement;
    /** Responde o que já foi pedido: os pedidos, as chegadas, a próxima tag e as peças do feixe. */
    const responder = () => {
      http.match(`${API}/pedidos`).forEach((r) => r.flush([PEDIDO]));
      http.match(`${API}/pedidos/1/chegadas`).forEach((r) => r.flush([CHEGADA]));
      http.match(`${API}/tags/proxima`).forEach((r) => r.flush({ tag: 'BR0000000040' }));
      http.match((r) => r.url === `${API}/dormentes` && r.method === 'GET').forEach((r) => r.flush([]));
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
    /** O pedido da tag do fornecedor, que sai na hora (ao escolher o feixe e depois de cadastrar), do feixe escolhido. */
    const pedidoDoFornecedor = (): TestRequest => {
      const r = http.expectOne((q) => q.url === `${API}/tags/proxima-fornecedor`);
      expect(r.request.params.get('idFeixe')).toBe('7');
      return r;
    };
    await atualizar();
    const botao = (texto: string) => [...tela.querySelectorAll('button')].find((b) => b.textContent?.includes(texto))!;
    const campo = (rotulo: string) =>
      [...tela.querySelectorAll('mat-form-field')].find((c) => c.querySelector('mat-label')?.textContent?.includes(rotulo))?.querySelector('input') as HTMLInputElement;
    return { fixture, tela, http, botao, campo, atualizar, pedidoDoFornecedor };
  };

  it('ao escolher o feixe, o campo já vem com SIGLA-NÚMERO DO PEDIDO-SEQUÊNCIA e continua editável', async () => {
    const { tela, botao, campo, atualizar, pedidoDoFornecedor } = await abrir();
    botao('cadastradas').click();
    pedidoDoFornecedor().flush({ tag: 'TG-20261017195108-03' });
    await atualizar();
    expect(tela.textContent).toContain('Tag do fornecedor (montada pelo pedido; confira)');
    expect(campo('Tag do fornecedor').value).toBe('TG-20261017195108-03');
    expect(campo('Tag do fornecedor').readOnly).toBe(false);
  });

  it('cadastra com a tag do fornecedor e, depois, a da peça seguinte já vem montada', async () => {
    const { http, botao, campo, atualizar, pedidoDoFornecedor } = await abrir();
    botao('cadastradas').click();
    pedidoDoFornecedor().flush({ tag: 'TG-20261017195108-03' });
    await atualizar();

    botao('Cadastrar peça').click();
    const envio = http.expectOne((r) => r.url === `${API}/dormentes` && r.method === 'POST');
    expect(envio.request.body).toMatchObject({ idFeixe: 7, tag: 'BR0000000040', tagFornecedor: 'TG-20261017195108-03' });
    envio.flush({ resultado: 'Ok', mensagem: 'Peça DM-E01-L01-0003 cadastrada.', peca: { codigo: 'DM-E01-L01-0003' } });
    pedidoDoFornecedor().flush({ tag: 'TG-20261017195108-04' });
    await atualizar();
    expect(campo('Tag do fornecedor').value).toBe('TG-20261017195108-04');
  });
});
