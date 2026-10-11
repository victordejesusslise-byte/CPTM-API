import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Indicadores } from './indicadores';

describe('Indicadores', () => {
  it('pede o período quando vem', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    TestBed.inject(Indicadores).listar('2026-10-01', null).subscribe();
    const req = TestBed.inject(HttpTestingController).expectOne((r) => r.url === 'http://localhost:5169/api/indicadores');
    expect(req.request.params.get('de')).toBe('2026-10-01');
    expect(req.request.params.has('ate')).toBe(false);
  });
});
