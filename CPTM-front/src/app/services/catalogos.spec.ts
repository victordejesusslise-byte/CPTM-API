import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Catalogos } from './catalogos';

describe('Catalogos', () => {
  it('busca os tamanhos uma vez só', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const catalogos = TestBed.inject(Catalogos);
    const http = TestBed.inject(HttpTestingController);
    catalogos.tamanhos().subscribe();
    http.expectOne('http://localhost:5169/api/catalogos/tamanhos').flush([]);
    catalogos.tamanhos().subscribe();
    http.expectNone('http://localhost:5169/api/catalogos/tamanhos');
  });
});
