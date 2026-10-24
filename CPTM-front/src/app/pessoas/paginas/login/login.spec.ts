import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { tokenDeTeste } from '../../../compartilhado/testes/sessao-de-teste';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let tela: HTMLElement;
  let http: HttpTestingController;

  const digitar = (seletor: string, valor: string) => {
    const campo = tela.querySelector<HTMLInputElement>(seletor)!;
    campo.value = valor;
    campo.dispatchEvent(new Event('input'));
  };
  const enviar = async () => {
    tela.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  };

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(Login);
    tela = fixture.nativeElement;
    http = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  it('não chama a API com o formulário vazio', async () => {
    await enviar();
    http.expectNone('http://localhost:8045/api/auth/login');
    expect(tela.textContent).toContain('Informe o e-mail.');
  });

  it('entra e vai para o painel', async () => {
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    digitar('input[type=email]', 'ana@cptm.sp.gov.br');
    digitar('input[type=password]', 'segredo');
    await enviar();
    http.expectOne('http://localhost:8045/api/auth/login').flush(tokenDeTeste());
    await vi.waitFor(() => expect(navegar).toHaveBeenCalledWith('/painel'));
  });

  it('senha errada mostra o aviso em vermelho', async () => {
    digitar('input[type=email]', 'ana@cptm.sp.gov.br');
    digitar('input[type=password]', 'errada');
    await enviar();
    http.expectOne('http://localhost:8045/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();
    expect(tela.querySelector('.aviso--erro')?.textContent).toContain('E-mail ou senha não conferem.');
  });
});
