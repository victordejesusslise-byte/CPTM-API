import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NO_APP } from '../ao-vivo/trabalho-da-noite';
import { LEITOR, LeitorSimulado } from './leitor-simulado';
import { LeitorUsb, TRANSPORTE_USB, TransporteUsb } from './leitor-usb';
import { PainelLeitor } from './painel-leitor';

/** O módulo no USB-C de mentira: PONG, uma etiqueta Feiju com o número BR0000000032. */
const firmware = (): TransporteUsb => {
  const ouvintes: ((l: string) => void)[] = [];
  const respostas: Record<string, string[]> = {
    PING: ['PONG;YBYRA-RFID-P0'],
    SCAN: ['TAG;UID=5350C0CEA40001;BYTES=7', 'TAG;TYPE=NFC_TYPE2_COMPATIVEL', 'NDEF;YBYRA', 'WRITE;PERMITIDO'],
    READ: ['READ;ID=BR0000000032'],
  };
  return {
    conectar: async () => undefined, desconectar: async () => undefined, aoMudar: () => undefined,
    aoLer: (f) => void ouvintes.push(f),
    enviar: async (linha) => void setTimeout(() => (respostas[linha] ?? []).forEach((l) => ouvintes.forEach((f) => f(l))), 1),
  };
};

describe('O leitor do app e o painel do módulo', () => {
  const configurar = (noApp: boolean) =>
    TestBed.configureTestingModule({
      imports: [PainelLeitor],
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: NO_APP, useValue: noApp }, { provide: TRANSPORTE_USB, useValue: firmware() }],
    });

  it('no app, as telas usam o módulo do USB-C; no site, o simulado', () => {
    configurar(true);
    expect(TestBed.inject(LEITOR)).toBeInstanceOf(LeitorUsb);
    TestBed.resetTestingModule();
    configurar(false);
    expect(TestBed.inject(LEITOR)).toBeInstanceOf(LeitorSimulado);
  });

  it('no app, o painel conecta, identifica a etiqueta e mostra a peça dela', async () => {
    configurar(true);
    const fixture = TestBed.createComponent(PainelLeitor);
    fixture.detectChanges();
    const tela = fixture.nativeElement as HTMLElement;
    const botao = (texto: string) => [...tela.querySelectorAll('button')].find((b) => b.textContent?.includes(texto))!;
    botao('Módulo').click();
    fixture.detectChanges();
    expect(tela.textContent).toContain('Desconectado');

    botao('Identificar etiqueta').click();
    const http = TestBed.inject(HttpTestingController);
    const peca = await vi.waitFor(() => {
      const r = http.match((q) => q.url.endsWith('/tags/BR0000000032'));
      if (!r.length) throw new Error('esperando a busca da peça');
      return r[0];
    });
    peca.flush({ id: 9, codigo: 'DM-E01-L02-0031' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(tela.textContent).toContain('YBYRA-RFID-P0');
    expect(tela.textContent).toContain('5350C0CEA40001');
    expect(tela.textContent).toContain('NFC Type 2');
    expect(tela.textContent).toContain('BR0000000032');
    expect(tela.textContent).toContain('DM-E01-L02-0031');
  });
});
