import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RelatorioAmbiental } from './relatorio-ambiental';

describe('RelatorioAmbiental', () => {
  it('gera o rascunho do mês escolhido', async () => {
    await TestBed.configureTestingModule({ imports: [RelatorioAmbiental], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(RelatorioAmbiental);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('http://localhost:5169/api/relatorios-ambientais').flush([]);
    await fixture.whenStable();
    const tela = fixture.nativeElement as HTMLElement;
    [...tela.querySelectorAll('button')].find((b) => b.textContent?.includes('Gerar o relatório do mês'))!.click();
    const req = http.expectOne((r) => r.url === 'http://localhost:5169/api/relatorios-ambientais' && r.method === 'POST');
    expect(req.request.params.get('mes')).toMatch(/^\d{4}-\d{2}-01$/);
    req.flush({ id: 1, mesReferencia: req.request.params.get('mes'), status: 'Rascunho', geradoEm: '2026-10-12T10:00:00', enviadoEm: null, volumeM3Estoque: 1.2, itens: [] });
    fixture.detectChanges();
    http.match((r) => r.method === 'GET').forEach((r) => r.flush([]));
    await fixture.whenStable();
    expect(tela.querySelector('.aviso--ok')?.textContent).toContain('Rascunho');
  });
});
