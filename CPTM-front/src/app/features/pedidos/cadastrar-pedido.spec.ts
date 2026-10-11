import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { entrarComo } from '../../testes/sessao-de-teste';
import { CadastrarPedido } from './cadastrar-pedido';

describe('CadastrarPedido', () => {
  it('sem fornecedor não vai para a revisão', async () => {
    localStorage.clear();
    entrarComo('OperadorPatio');
    await TestBed.configureTestingModule({
      imports: [CadastrarPedido],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(CadastrarPedido);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.match(() => true).forEach((r) => r.flush([]));
    await fixture.whenStable();
    const tela = fixture.nativeElement as HTMLElement;
    expect(tela.textContent).toContain('passo 1 de 2');
    tela.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    expect(tela.textContent).not.toContain('Revise o pedido');
    expect(tela.querySelector('.aviso--erro')?.textContent).toContain('Escolha o fornecedor');
  });
});
