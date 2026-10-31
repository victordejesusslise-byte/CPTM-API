import { TestBed } from '@angular/core/testing';
import { CaixaDeToasts } from './caixa-de-toasts';
import { Toasts } from './toasts';

describe('CaixaDeToasts (Victor, 2026-10-31: "coloque os toasts de erro nas telas, e de aprovação também")', () => {
  const abrir = async () => {
    await TestBed.configureTestingModule({ imports: [CaixaDeToasts] }).compileComponents();
    const fixture = TestBed.createComponent(CaixaDeToasts);
    fixture.detectChanges();
    await fixture.whenStable();
    return { fixture, toasts: TestBed.inject(Toasts) };
  };
  const aviso = (classe: string, papel: string | null, texto: string) => {
    const p = document.createElement('p');
    p.className = 'aviso ' + classe;
    if (papel) p.setAttribute('role', papel);
    p.textContent = texto;
    document.body.appendChild(p);
    return p;
  };
  afterEach(() => document.querySelectorAll('p.aviso').forEach((p) => p.remove()));

  it('o erro e a confirmação que a tela mostra viram toast', async () => {
    const { fixture, toasts } = await abrir();
    aviso('aviso--erro', 'alert', 'Seu cargo não pode fazer isso.');
    aviso('aviso--ok', 'status', 'Troca aprovada. O pátio já pode separar a peça.');
    await vi.waitFor(() => expect(toasts.lista().length).toBe(2));
    fixture.detectChanges();
    const caixa = fixture.nativeElement as HTMLElement;
    expect(caixa.querySelector('.toast--erro')?.textContent).toContain('Seu cargo não pode fazer isso.');
    expect(caixa.querySelector('.toast--ok')?.textContent).toContain('Troca aprovada');
  });

  it('a mensagem que muda vira outro toast; a mesma repetida não; o aviso fixo de lista não vira', async () => {
    const { toasts } = await abrir();
    const p = aviso('aviso--erro', 'alert', 'Para reprovar, escreva o motivo.');
    aviso('aviso--erro', null, 'Fora do lugar: tag BR1 é da pilha A01');
    await vi.waitFor(() => expect(toasts.lista().length).toBe(1));
    p.textContent = 'Para reprovar, escreva o motivo.';
    p.firstChild!.nodeValue = 'Só a troca pedida é aprovada ou reprovada.';
    await vi.waitFor(() => expect(toasts.lista().map((t) => t.texto)).toEqual(['Para reprovar, escreva o motivo.', 'Só a troca pedida é aprovada ou reprovada.']));
  });

  it('fecha no X', async () => {
    const { fixture, toasts } = await abrir();
    toasts.mostrar('Sem conexão com o servidor.', 'erro');
    fixture.detectChanges();
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.toast button')!.click();
    expect(toasts.lista()).toEqual([]);
  });
});
