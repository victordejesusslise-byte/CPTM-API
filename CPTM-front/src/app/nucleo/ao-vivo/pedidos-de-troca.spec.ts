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
  const dosLotes = (r: { url: string; params: { get(nome: string): string | null } }) => r.url.endsWith('/api/lotes-troca') && r.params.get('status') === 'Pedido';

  it('o técnico também aprova: conta os pedidos que esperam e conta de novo quando pedem', async () => {
    const { pedidos, http } = abrir('Tecnico');
    http.expectOne(dosPedidos).flush([{ id: 1 }, { id: 2 }]);
    http.expectOne(dosLotes).flush([]);
    await vi.waitFor(() => expect(pedidos.quantas()).toBe(2));

    const contou = pedidos.contar();
    http.expectOne(dosPedidos).flush([{ id: 1 }, { id: 2 }, { id: 3 }]);
    http.expectOne(dosLotes).flush([]);
    await contou;
    expect(pedidos.quantas()).toBe(3);
  });

  it('conta também os lotes de troca pedidos', async () => {
    const { pedidos, http } = abrir('Supervisor');
    http.expectOne(dosPedidos).flush([{ id: 1 }]);
    http.expectOne(dosLotes).flush([{ id: 9 }, { id: 10 }]);
    await vi.waitFor(() => expect(pedidos.quantas()).toBe(3));
  });

  it('o operador da via não aprova: não conta', () => {
    const { pedidos, http } = abrir('OperadorVia');
    http.expectNone(dosPedidos);
    expect(pedidos.quantas()).toBe(0);
  });
});
