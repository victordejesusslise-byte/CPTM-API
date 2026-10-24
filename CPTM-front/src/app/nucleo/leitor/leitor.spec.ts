import { TestBed } from '@angular/core/testing';
import { detalhesDoModulo, escolherAlvo } from './leitor';
import { LeitorSimulado } from './leitor-simulado';

describe('Leitor (o fluxo do módulo, simulado)', () => {
  const leitor = () => TestBed.inject(LeitorSimulado);

  it('o alvo único: a mais forte só vale com as outras abaixo da folga', () => {
    const vistas = [{ tag: 'A', rssiDbm: -45 }, { tag: 'B', rssiDbm: -55 }];
    expect(escolherAlvo(vistas, 12)).toEqual({ tipo: 'duvida', tagsVistas: 2 }); // Armazém: 10 dB não basta
    expect(escolherAlvo(vistas, 9)).toEqual({ tipo: 'alvo', tag: 'A', rssiDbm: -45 }); // Normal
    expect(escolherAlvo([], 6)).toEqual({ tipo: 'nenhuma' });
  });

  it('lê a peça à frente com o modo, a potência e o sinal, e a leitura da API leva junto', async () => {
    leitor().montar('vizinhas', 'BR0000000002');
    const r = await leitor().ler();
    expect(r).toEqual({ ok: true, leitura: { tag: 'BR0000000002', idModo: 2, potenciaDbm: 18, rssiDbm: -45, tagsVistas: 1 } });
    expect(detalhesDoModulo('BR0000000002')).toEqual({ idModo: 2, potenciaDbm: 18, rssiDbm: -45, tagsVistas: 1 });
    expect(detalhesDoModulo('BR0000000009')).toEqual({});
  });

  it('com duas peças de sinal parecido, sem etiqueta ou sem o módulo, não lê e diz por quê', async () => {
    leitor().montar('duvida', 'BR0000000002');
    expect(await leitor().ler()).toEqual({ ok: false, motivo: 'Mais de um dormente próximo. Aproxime ou direcione melhor.' });
    leitor().montar('nenhuma', '');
    expect((await leitor().ler()).ok).toBe(false);
    leitor().montar('uma', 'BR0000000002');
    leitor().desconectar();
    expect(await leitor().ler()).toEqual({ ok: false, motivo: 'Conecte o módulo no celular.' });
  });

  it('a chave A/N/V muda a folga: vizinha a 10 dB atrapalha no Armazém e não na Via', async () => {
    leitor().porPerto.set([{ tag: 'BR0000000002', rssiDbm: -45 }, { tag: 'BR0000000003', rssiDbm: -55 }]);
    leitor().trocarModo('A');
    expect((await leitor().ler()).ok).toBe(false);
    leitor().trocarModo('V');
    expect((await leitor().ler()).ok).toBe(true);
  });

  it('grava o número novo na etiqueta à frente e relê; com outra no alcance, não grava', async () => {
    leitor().montar('uma', 'BR0000000500');
    expect(await leitor().gravarEReler('BR0000000777')).toEqual({ ok: true, relida: 'BR0000000777' });
    leitor().montar('duvida', 'BR0000000500');
    expect((await leitor().gravarEReler('BR0000000778')).ok).toBe(false);
  });
});
