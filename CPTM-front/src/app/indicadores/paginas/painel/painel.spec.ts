import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Routes, provideRouter } from '@angular/router';
import { IndicadorDto } from '../../entidades/indicador';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { Painel } from './painel';

const indicador = (chave: string, vermelho: boolean): IndicadorDto => ({
  chave, nome: `Indicador ${chave}`, valor: 12.5, unidade: '%', resumo: 'resumo', referencia: 'até 10%', vermelho,
  detalhes: [{ rotulo: 'Peças', valor: '8' }],
});

describe('Painel', () => {
  const abrir = async (cargo: Parameters<typeof entrarComo>[0], aba: 'hoje' | 'numeros' = 'numeros', rotas: Routes = []) => {
    localStorage.clear();
    entrarComo(cargo);
    await TestBed.configureTestingModule({
      imports: [Painel],
      providers: [provideRouter(rotas), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(Painel);
    fixture.componentRef.setInput('aba', aba);
    // o indicador fica pendente até a resposta: whenStable só depois do flush
    fixture.detectChanges();
    return { fixture, tela: fixture.nativeElement as HTMLElement, http: TestBed.inject(HttpTestingController) };
  };

  it('mostra os indicadores e marca em vermelho só o que pede ação', async () => {
    const { fixture, tela, http } = await abrir('Supervisor');
    http.expectOne((r) => r.url === 'http://localhost:8045/api/indicadores').flush([indicador('a', true), indicador('b', false)]);
    await fixture.whenStable();
    expect(tela.querySelectorAll('.indicador')).toHaveLength(2);
    expect(tela.querySelectorAll('.indicador--vermelho')).toHaveLength(1);
    expect(tela.textContent).toContain('1 indicador pede atenção.');
  });

  it('mostra os atalhos só do cargo', async () => {
    const { fixture, tela, http } = await abrir('OperadorVia', 'hoje');
    http.expectOne((r) => r.url === 'http://localhost:8045/api/indicadores').flush([]);
    await fixture.whenStable();
    const atalhos = [...tela.querySelectorAll('.atalho')].map((a) => a.textContent ?? '');
    expect(atalhos.some((a) => a.includes('Alertas'))).toBe(true);
    expect(atalhos.some((a) => a.includes('Pedidos'))).toBe(false);
  });

  it('mostra o erro da API', async () => {
    const { fixture, tela, http } = await abrir('Supervisor');
    http.expectOne((r) => r.url === 'http://localhost:8045/api/indicadores').flush({ detail: 'Período inválido.' }, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();
    expect(tela.querySelector('.aviso--erro')?.textContent).toContain('Período inválido.');
  });

  // A casca com a rota dos pedidos, só para supervisor e operador de pátio (como no app.routes).
  const rotas: Routes = [{ path: '', children: [{ path: 'pedidos', data: { cargos: ['Supervisor', 'OperadorPatio'] }, children: [] }] }];

  it('põe em destaque o que pede atenção, com o botão para verificar', async () => {
    const { fixture, tela, http } = await abrir('Supervisor', 'hoje', rotas);
    http.expectOne((r) => r.url === 'http://localhost:8045/api/indicadores').flush([indicador('estoque-m3', false), indicador('segunda-classe-pedido', true)]);
    await fixture.whenStable();
    const destaque = tela.querySelector('.atencao')!;
    expect(destaque.querySelectorAll('.indicador--vermelho')).toHaveLength(1);
    expect(destaque.querySelector('a.indicador__verificar')?.getAttribute('href')).toBe('/pedidos');
    expect(destaque.textContent).toContain('Precisa de atenção');
  });

  it('no acompanhamento, o vermelho vem primeiro e só tem botão se o cargo abre a tela', async () => {
    const { fixture, tela, http } = await abrir('Especialista', 'numeros', rotas);
    http.expectOne((r) => r.url === 'http://localhost:8045/api/indicadores').flush([indicador('estoque-m3', false), indicador('segunda-classe-pedido', true)]);
    await fixture.whenStable();
    expect(tela.querySelector('.indicador')?.classList).toContain('indicador--vermelho');
    expect(tela.querySelector('.indicador__verificar')).toBeNull();
  });
});
