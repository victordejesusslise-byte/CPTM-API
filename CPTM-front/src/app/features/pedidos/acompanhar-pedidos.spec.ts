import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PedidoDto } from '../../models/pedido';
import { entrarComo } from '../../testes/sessao-de-teste';
import { AcompanharPedidos } from './acompanhar-pedidos';

const hoje = new Date().toISOString().slice(0, 10);
const pedido = (id: number, codigo: string, quantidade: number, recebidas: number, situacao: string): PedidoDto => ({
  id, codigo, idEmpresa: 1, empresa: 'TG Três Guri', idContrato: null, contrato: null, dataCompra: hoje, criadoEm: hoje + 'T10:00:00',
  quantidade, volumeM3: 1, status: 'Aberto', situacao, pecasRecebidas: recebidas,
  segundaClasse: { valido: true, percentual: 0, unidade: 'm3', limite: 20, mensagem: '2ª classe: 0%' }, itens: [],
});

describe('AcompanharPedidos', () => {
  it('soma o filtro, marca a entrega incompleta e filtra pelo que falta chegar', async () => {
    localStorage.clear();
    entrarComo('Supervisor');
    await TestBed.configureTestingModule({
      imports: [AcompanharPedidos],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(AcompanharPedidos);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController).expectOne('http://localhost:5169/api/pedidos')
      .flush([pedido(1, 'PD-1', 30, 10, 'RecebidoParcial'), pedido(2, 'PD-2', 20, 20, 'Recebido'), pedido(3, 'PD-3', 15, 0, 'Aberto')]);
    await fixture.whenStable();
    const tela = fixture.nativeElement as HTMLElement;
    const totais = [...tela.querySelectorAll('.totais .t b')].map((b) => b.textContent?.trim());
    expect(totais).toEqual(['65', '30', '35', '1']);
    expect(tela.querySelectorAll('tbody tr')).toHaveLength(3);
    expect(tela.querySelector('.totais .t b.vermelho')?.textContent?.trim()).toBe('1');
  });
});
