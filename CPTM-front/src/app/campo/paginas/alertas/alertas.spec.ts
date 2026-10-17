import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AlertaDto } from '../../entidades/campo';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { Alertas } from './alertas';

const alerta: AlertaDto = {
  id: 7, origem: 'Sensor', tipo: 'TagMuda', status: 'Aberto', data: '2026-10-11T02:10:00', desvioMm: null, km: 213.12,
  idDormente: 3, codigoPeca: 'DM-01', tag: 'BR0000000003', deteccao: null, vistorias: 0, idUsuarioDecisao: null, dataDecisao: null, motivoDescarte: null,
};

describe('Alertas', () => {
  const abrir = async (aba: 'sensores' | 'tags', aberto?: string) => {
    localStorage.clear();
    entrarComo('OperadorVia');
    await TestBed.configureTestingModule({ imports: [Alertas], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(Alertas);
    fixture.componentRef.setInput('aba', aba);
    if (aberto) fixture.componentRef.setInput('aberto', aberto);
    fixture.detectChanges();
    return { fixture, tela: fixture.nativeElement as HTMLElement, http: TestBed.inject(HttpTestingController) };
  };

  it('lista os alertas abertos dos sensores', async () => {
    const { fixture, tela, http } = await abrir('sensores');
    const req = http.expectOne((r) => r.url === 'http://localhost:5169/api/alertas');
    expect(req.request.params.get('origem')).toBe('Sensor');
    expect(req.request.params.get('status')).toBe('Aberto');
    req.flush([alerta]);
    await fixture.whenStable();
    expect(tela.querySelector('.cartao-alerta')?.textContent).toContain('Tag não respondeu');
  });

  it('a vistoria sem foto não vai para a API', async () => {
    const { fixture, tela, http } = await abrir('sensores', '7');
    http.match(() => true).forEach((r) => r.flush(r.request.url.endsWith('/alertas/7') ? alerta : [alerta]));
    await fixture.whenStable();
    const tagCampo = tela.querySelector<HTMLInputElement>('app-leitura-tag input')!;
    tagCampo.value = 'BR0000000003';
    tagCampo.dispatchEvent(new Event('input'));
    [...tela.querySelectorAll('button')].find((b) => b.textContent?.includes('Registrar vistoria'))!.click();
    await fixture.whenStable();
    http.expectNone((r) => r.url.endsWith('/vistoria'));
    expect(tela.querySelector('.aviso--erro')?.textContent).toContain('Tire a foto');
  });
});
