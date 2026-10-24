import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { App } from './app';
import { NO_APP } from './nucleo/ao-vivo/trabalho-da-noite';

@Component({ template: '<p class="tela-login">login</p>' })
class LoginDeTeste {}

describe('App', () => {
  it('mostra só a saída do roteador', async () => {
    await TestBed.configureTestingModule({ imports: [App], providers: [provideRouter([])] }).compileComponents();
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelector('router-outlet')).not.toBeNull();
  });

  it('enquanto o login carrega, mostra o skeleton do login no lugar da tela vazia', async () => {
    let carregar!: () => void;
    const login = new Promise<typeof LoginDeTeste>((pronto) => (carregar = () => pronto(LoginDeTeste)));
    await TestBed.configureTestingModule({ imports: [App], providers: [provideRouter([{ path: 'login', loadComponent: () => login }])] }).compileComponents();
    const fixture = TestBed.createComponent(App);
    const tela = fixture.nativeElement as HTMLElement;
    void TestBed.inject(Router).navigateByUrl('/login');
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(tela.querySelector('.inicio--login')).not.toBeNull();
    });
    expect(tela.textContent).toContain('Abrindo o YBYRA');

    carregar();
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(tela.querySelector('.tela-login')).not.toBeNull();
    });
    expect(tela.querySelector('app-esqueleto-inicio')).toBeNull();
  });

  it('no app, o skeleton do login fica 3 segundos na frente do login', async () => {
    vi.useFakeTimers();
    try {
      await TestBed.configureTestingModule({
        imports: [App], providers: [provideRouter([{ path: 'login', component: LoginDeTeste }]), { provide: NO_APP, useValue: true }],
      }).compileComponents();
      const fixture = TestBed.createComponent(App);
      const tela = fixture.nativeElement as HTMLElement;
      await TestBed.inject(Router).navigateByUrl('/login');
      fixture.detectChanges();
      expect(tela.querySelector('.tela-login')).not.toBeNull();
      expect(tela.querySelector('.inicio--login')).not.toBeNull();
      await vi.advanceTimersByTimeAsync(2900);
      fixture.detectChanges();
      expect(tela.querySelector('.inicio--login')).not.toBeNull();
      await vi.advanceTimersByTimeAsync(200);
      fixture.detectChanges();
      expect(tela.querySelector('app-esqueleto-inicio')).toBeNull();
    } finally {
      vi.useRealTimers();
    }
  });
});
