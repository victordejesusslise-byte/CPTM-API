import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CadastrarUsuario } from './cadastrar-usuario';

describe('CadastrarUsuario', () => {
  it('manda nome, e-mail, cargo e senha para a API e avisa quem foi cadastrado', async () => {
    await TestBed.configureTestingModule({
      imports: [CadastrarUsuario],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(CadastrarUsuario);
    await fixture.whenStable();
    const tela = fixture.nativeElement as HTMLElement;
    const componente = fixture.componentInstance as unknown as { form: { setValue: (v: object) => void } };
    componente.form.setValue({ nome: 'Bia Lima', email: 'bia@cptm.sp.gov.br', cargo: 'Tecnico', senha: 'segredo123' });
    tela.querySelector('form')!.dispatchEvent(new Event('submit'));
    const req = TestBed.inject(HttpTestingController).expectOne('http://localhost:8045/api/usuarios');
    expect(req.request.body).toEqual({ nome: 'Bia Lima', email: 'bia@cptm.sp.gov.br', senha: 'segredo123', cargo: 'Tecnico' });
    req.flush({ id: 2, nome: 'Bia Lima', email: 'bia@cptm.sp.gov.br', cargo: 'Tecnico', ativo: true });
    await fixture.whenStable();
    expect(tela.querySelector('.aviso--ok')?.textContent).toContain('Bia Lima cadastrado como Técnico');
  });
});
