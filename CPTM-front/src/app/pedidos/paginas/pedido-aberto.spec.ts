import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PedidoDto } from '../entidades/pedido';
import { entrarComo } from '../../compartilhado/testes/sessao-de-teste';
import { PedidoAberto } from './pedido-aberto';

// como o DEMO da foto do Victor: pediu 8, chegaram 4, toda a 2ª classe (fora do limite, mas já cadastrado e travado)
const pedido: PedidoDto = {
  id: 9, codigo: 'PD-9', idEmpresa: 1, empresa: 'TG–Três Guri', idContrato: null, contrato: null, dataCompra: '2026-10-08', criadoEm: '2026-10-08T10:00:00',
  quantidade: 8, volumeM3: 0.914, status: 'Aberto', situacao: 'RecebidoParcial', pecasRecebidas: 4,
  segundaClasse: { valido: false, percentual: 100, unidade: 'm3', limite: 20, mensagem: '2ª classe: 100% do m³ (limite de 20%). Reduza a 2ª classe para registrar o pedido.' },
  itens: [],
};

describe('PedidoAberto (Victor, 2026-10-31)', () => {
  it('no pedido cadastrado não aparece o aviso da 2ª classe; chegou em parte, oferece cadastrar a chegada do restante', async () => {
    localStorage.clear();
    entrarComo('Supervisor');
    await TestBed.configureTestingModule({
      imports: [PedidoAberto],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(PedidoAberto);
    fixture.componentRef.setInput('id', 9);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    // o pedido chega e puxa os contratos do fornecedor: responde em rodadas, sem esperar o "estável" (ele esperaria a próxima busca)
    for (let i = 0; i < 3; i++) {
      http.match((r) => /\/pedidos\/9$/.test(r.url)).forEach((r) => r.flush(pedido));
      http.match(() => true).forEach((r) => r.flush([]));
      await new Promise((r) => setTimeout(r));
      fixture.detectChanges();
    }
    const tela = fixture.nativeElement as HTMLElement;
    expect(tela.textContent).toContain('Travado');
    expect(tela.textContent).not.toContain('Reduza a 2ª classe');
    const restante = [...tela.querySelectorAll<HTMLAnchorElement>('a')].find((a) => a.textContent?.includes('Cadastrar a chegada do restante'));
    expect(restante?.textContent).toContain('4 peças');
    expect(restante?.getAttribute('href')).toBe('/chegadas?pedido=9&restante=4');
  });
});
