import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LerPlaca } from './ler-placa';

// Os 9 campos da placa aparecem sempre e são obrigatórios (Victor, 2026-11-15): o que o OCR não leu, o operador digita.
describe('LerPlaca', () => {
  const campo = (id: number, nome: string, valor: string) => ({
    id, campo: nome, valorLido: valor, confianca: 90, conferePedido: true, pronto: false, valorConfirmado: null, corrigido: null,
  });

  it('mostra os 9 campos, marca os não lidos e só confirma com todos preenchidos', async () => {
    await TestBed.configureTestingModule({ imports: [LerPlaca], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(LerPlaca);
    fixture.componentRef.setInput('idPedido', 12);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const tela = fixture.nativeElement as HTMLElement;
    const botao = (texto: string) => [...tela.querySelectorAll('button')].find((b) => b.textContent?.includes(texto))!;

    // Lê a placa: o OCR achou só o fornecedor e a ata.
    (fixture.componentInstance as unknown as { foto: { set(f: File): void } }).foto.set(new File([new Uint8Array([1])], 'placa.jpg', { type: 'image/jpeg' }));
    botao('Ler a placa').click();
    await new Promise((pronto) => setTimeout(pronto));
    http.expectOne((r) => r.url.endsWith('/ocr/placas') && r.method === 'POST').flush({
      id: 7, fotoArquivo: 'x.jpg', textoLido: null, confiancaMedia: 90, versaoModelo: 1, confirmada: false,
      campos: [campo(1, 'Fornecedor', 'TG–Três Guri'), campo(2, 'Ata', 'AR00224')],
    });
    fixture.detectChanges();

    const caixas = [...tela.querySelectorAll<HTMLInputElement>('input.campo-tabela')];
    expect(caixas.length).toBe(9);
    expect([...tela.querySelectorAll('td')].filter((td) => td.textContent?.trim() === 'não lido').length).toBe(7);
    expect(tela.querySelector('.aviso--ok')?.textContent).toContain('Digite os 7 campos');

    // Confirmar com campo vazio: avisa o que falta e não manda nada.
    botao('Confirmar a placa').click();
    fixture.detectChanges();
    expect(tela.querySelector('.aviso--erro')?.textContent).toContain('Espécie e classe');
    http.expectNone((r) => r.url.endsWith('/confirmacao'));

    // Preenchidos os 9, confirma mandando os 9.
    for (const c of caixas.filter((c) => !c.value)) {
      c.value = 'X';
      c.dispatchEvent(new Event('input'));
    }
    botao('Confirmar a placa').click();
    const req = http.expectOne((r) => r.url.endsWith('/ocr/placas/7/confirmacao'));
    expect((req.request.body as { campos: unknown[] }).campos.length).toBe(9);
  });
});
