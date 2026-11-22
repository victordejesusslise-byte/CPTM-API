import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RolarAoAbrir } from './rolar-ao-abrir';

// Uma tela de teste: a lista é longa e o detalhe aparece quando um item abre (a chave é o id do item).
@Component({
  imports: [RolarAoAbrir],
  template: `<div style="height: 2000px">lista</div>
    @if (aberto(); as id) { <section [rolarAoAbrir]="id">detalhe {{ id }}</section> }`,
})
class Tela {
  readonly aberto = signal<number | null>(null);
}

describe('Rolar até o detalhe aberto (celular)', () => {
  let rolar: ReturnType<typeof vi.fn>;

  // Finge a largura da tela: celular (até 820 px) ou computador.
  function tela(celular: boolean): void {
    vi.stubGlobal('matchMedia', (q: string) => ({ matches: celular && q.includes('max-width'), media: q, addEventListener() {}, removeEventListener() {} }));
  }

  beforeEach(() => {
    rolar = vi.fn();
    Element.prototype.scrollIntoView = rolar as unknown as Element['scrollIntoView'];
  });
  afterEach(() => vi.unstubAllGlobals());

  async function abrir(id: number | null, f = TestBed.createComponent(Tela)) {
    f.componentInstance.aberto.set(id);
    await f.whenStable();
    return f;
  }

  it('no celular, rola até o começo do detalhe quando ele abre fora da vista', async () => {
    tela(true);
    await abrir(1);
    expect(rolar).toHaveBeenCalledTimes(1);
    expect(rolar).toHaveBeenCalledWith({ behavior: 'smooth', block: 'start' });
  });

  it('rola de novo quando outro item abre no mesmo detalhe', async () => {
    tela(true);
    const f = await abrir(1);
    await abrir(2, f);
    expect(rolar).toHaveBeenCalledTimes(2);
  });

  it('não rola quando o começo do detalhe já está na tela', async () => {
    tela(true);
    vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockReturnValue({ top: 200, bottom: 400, left: 0, right: 390, width: 390, height: 200, x: 0, y: 200, toJSON() {} } as DOMRect);
    await abrir(1);
    expect(rolar).not.toHaveBeenCalled();
    vi.restoreAllMocks();
  });

  it('no computador, nada muda', async () => {
    tela(false);
    await abrir(1);
    expect(rolar).not.toHaveBeenCalled();
  });
});
