import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { entrarComo } from '../../compartilhado/testes/sessao-de-teste';
import { PedidosDeTroca } from './pedidos-de-troca';

describe('PedidosDeTroca (o número em "Aprovar trocas")', () => {
  const abrir = (cargo: Parameters<typeof entrarComo>[0]) => {
    localStorage.clear();
    entrarComo(cargo);
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const pedidos = TestBed.inject(PedidosDeTroca);
    TestBed.tick();
    return { pedidos, http: TestBed.inject(HttpTestingController) };
  };
  const dosPedidos = (r: { url: string; params: { get(nome: string): string | null } }) => r.url.endsWith('/api/trocas') && r.params.get('status') === 'Pedida';

  it('o técnico também aprova: conta os pedidos que esperam e conta de novo quando pedem', async () => {
    const { pedidos, http } = abrir('Tecnico');
    http.expectOne(dosPedidos).flush([{ id: 1 }, { id: 2 }]);
    await vi.waitFor(() => expect(pedidos.quantas()).toBe(2));

    const contou = pedidos.contar();
    http.expectOne(dosPedidos).flush([{ id: 1 }, { id: 2 }, { id: 3 }]);
    await contou;
    expect(pedidos.quantas()).toBe(3);
  });

  it('o operador da via não aprova: não conta', () => {
    const { pedidos, http } = abrir('OperadorVia');
    http.expectNone(dosPedidos);
    expect(pedidos.quantas()).toBe(0);
  });
});
