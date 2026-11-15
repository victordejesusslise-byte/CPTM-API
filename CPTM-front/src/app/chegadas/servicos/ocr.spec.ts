import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ESPERA_DO_OCR_MS, Ocr } from './ocr';

// O operador tira a foto, espera e confere (Victor, 2026-11-15): a leitura não pode passar de 45 s no app.
describe('Ocr', () => {
  it('espera o servidor no máximo 45 s', () => {
    expect(ESPERA_DO_OCR_MS).toBeLessThanOrEqual(45_000);
  });

  it('manda a foto com o nome, o pedido e o id do aparelho', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const ocr = TestBed.inject(Ocr);
    const http = TestBed.inject(HttpTestingController);
    const foto = new File([new Uint8Array([1, 2, 3])], 'placa.jpg', { type: 'image/jpeg' });

    let lida = 0;
    ocr.ler(foto, 12).subscribe((l) => (lida = l.id));
    // A foto é reduzida antes de subir (assíncrono): o pedido sai depois.
    await new Promise((pronto) => setTimeout(pronto));
    const req = http.expectOne((r) => r.url.endsWith('/ocr/placas') && r.method === 'POST');
    const corpo = req.request.body as FormData;
    expect((corpo.get('foto') as File).name).toBe('placa.jpg');
    expect(corpo.get('idPedido')).toBe('12');
    expect(corpo.get('idCliente')).toBeTruthy();
    req.flush({ id: 7, fotoArquivo: 'x.jpg', textoLido: null, confiancaMedia: null, versaoModelo: 1, confirmada: false, campos: [] });
    expect(lida).toBe(7);
  });
});
