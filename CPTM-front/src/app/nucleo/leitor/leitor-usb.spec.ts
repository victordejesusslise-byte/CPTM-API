import { TestBed } from '@angular/core/testing';
import { MODOS_PADRAO } from './leitor';
import { LeitorUsb, TRANSPORTE_USB, TransporteUsb } from './leitor-usb';
import { lerScan } from './protocolo-p0';

/** O firmware YBYRA-RFID-P0 v0.5 de mentira: responde a cada comando com as linhas que o de verdade manda (Victor.txt, 2026-11-11). */
class FirmwareFalso implements TransporteUsb {
  respostas: Record<string, string[]> = {
    PING: ['PONG;YBYRA-RFID-P0'],
    SCAN: ['TAG;UID=5350C0CEA40001;BYTES=7', 'TAG;TYPE=NFC_TYPE2_COMPATIVEL', 'NDEF;YBYRA', 'WRITE;PERMITIDO'],
    READ: ['READ;ID=BR0000000032'],
    WRITE: ['WRITE;OK;ID=BR0000000032', 'VERIFY;OK'],
  };
  semModulo = false;
  readonly enviados: string[] = [];
  private readonly ouvintes: ((linha: string) => void)[] = [];
  private readonly mudancas: ((ligado: boolean) => void)[] = [];

  async conectar(): Promise<void> {
    if (this.semModulo) throw new Error('Conecte o módulo no celular.');
    this.mudancas.forEach((f) => f(true));
  }
  async desconectar(): Promise<void> {
    this.mudancas.forEach((f) => f(false));
  }
  async enviar(linha: string): Promise<void> {
    this.enviados.push(linha);
    const chave = linha.startsWith('WRITE ') ? 'WRITE' : linha;
    setTimeout(() => (this.respostas[chave] ?? ['ERRO;COMANDO_INVALIDO']).forEach((l) => this.ouvintes.forEach((f) => f(l))), 1);
  }
  aoLer(f: (linha: string) => void): void {
    this.ouvintes.push(f);
  }
  aoMudar(f: (ligado: boolean) => void): void {
    this.mudancas.push(f);
  }
}

describe('LeitorUsb (o módulo YBYRA-RFID-P0 pelo USB-C)', () => {
  const abrir = (firmware = new FirmwareFalso()) => {
    TestBed.configureTestingModule({ providers: [{ provide: TRANSPORTE_USB, useValue: firmware }] });
    return { leitor: TestBed.inject(LeitorUsb), firmware };
  };

  it('lê o BR da etiqueta (PING, SCAN e READ) e manda como leitura do módulo, na distância normal', async () => {
    const { leitor, firmware } = abrir();
    const r = await leitor.ler();
    expect(r).toEqual({ ok: true, leitura: { tag: 'BR0000000032', idModo: MODOS_PADRAO.N.idModo, potenciaDbm: null, rssiDbm: null, tagsVistas: 1 } });
    expect(firmware.enviados).toEqual(['PING', 'SCAN', 'READ']);
    expect(leitor.conectado()).toBe(true);
    expect(leitor.modo().chave).toBe('N');
    expect(leitor.etiqueta()).toMatchObject({ uid: '5350C0CEA40001', tipo: 'NFC Type 2', id: 'BR0000000032' });
  });

  it('ligou o módulo no USB-C (o Android abriu a porta): confere sozinho com PING', async () => {
    const { leitor, firmware } = abrir();
    await firmware.conectar();
    await vi.waitFor(() => expect(leitor.conectado()).toBe(true));
    expect(leitor.firmware()).toBe('YBYRA-RFID-P0');
    await firmware.desconectar();
    expect(leitor.conectado()).toBe(false);
  });

  it('sem o módulo no USB-C, pede para conectar', async () => {
    const firmware = new FirmwareFalso();
    firmware.semModulo = true;
    const { leitor } = abrir(firmware);
    expect(await leitor.ler()).toEqual({ ok: false, motivo: 'Conecte o módulo no celular.' });
  });

  it('nenhuma etiqueta perto, ou etiqueta sem número do YBYRA: diz o porquê', async () => {
    const { leitor, firmware } = abrir();
    firmware.respostas['SCAN'] = ['ERRO;TAG_AUSENTE'];
    expect(await leitor.ler()).toEqual({ ok: false, motivo: expect.stringContaining('Nenhuma etiqueta') });
    firmware.respostas['SCAN'] = ['TAG;UID=E9BE153A;BYTES=4', 'TAG;TYPE=MIFARE_CLASSIC_COMPATIVEL', 'WRITE;PERMITIDO'];
    firmware.respostas['READ'] = ['READ;HEX=00000000000000000000000000000000', 'READ;SEM_ID_YBYRA'];
    expect(await leitor.ler()).toEqual({ ok: false, motivo: expect.stringContaining('não tem número do YBYRA') });
  });

  it('grava só com o VERIFY;OK do hardware e relê o número', async () => {
    const { leitor, firmware } = abrir();
    expect(await leitor.gravarEReler('BR0000000032')).toEqual({ ok: true, relida: 'BR0000000032' });
    expect(firmware.enviados.slice(-3)).toEqual(['SCAN', 'WRITE BR0000000032', 'READ']);
  });

  it('etiqueta não liberada para gravação: nem manda o WRITE', async () => {
    const { leitor, firmware } = abrir();
    firmware.respostas['SCAN'] = ['TAG;UID=B9047AE6;BYTES=4', 'TAG;TYPE=MIFARE_CLASSIC_COMPATIVEL', 'WRITE;BLOQUEADO_NESTA_TAG'];
    const r = await leitor.gravarEReler('BR0000000031');
    expect(r).toEqual({ ok: false, motivo: expect.stringContaining('não está liberada para gravação') });
    expect(firmware.enviados.some((l) => l.startsWith('WRITE'))).toBe(false);
  });

  it('a gravação que não se confirma não conta, e o número fora do padrão nem vai', async () => {
    const { leitor, firmware } = abrir();
    firmware.respostas['WRITE'] = ['ERRO;WRITE_PAGE=8', 'INFO;NDEF_PODE_ESTAR_INCOMPLETO;REPETIR_WRITE_MESMA_TAG', 'ERRO;WRITE_OU_VERIFY_FALHOU'];
    expect(await leitor.gravarEReler('BR0000000032')).toEqual({ ok: false, motivo: expect.stringContaining('não se confirmou') });
    expect(await leitor.gravarEReler('DM-0002')).toEqual({ ok: false, motivo: expect.stringContaining('BR e 10 números') });
  });

  it('entende as linhas do SCAN das duas etiquetas do protótipo', () => {
    expect(lerScan(['TAG;UID=E9BE153A;BYTES=4', 'TAG;TYPE=MIFARE_CLASSIC_COMPATIVEL', 'WRITE;PERMITIDO']))
      .toEqual({ uid: 'E9BE153A', tipo: 'MIFARE Classic', gravavel: true });
    expect(lerScan(['TAG;UID=X;BYTES=7', 'TAG;TYPE=NAO_SUPORTADO'])).toEqual({ erro: 'TIPO_NAO_SUPORTADO' });
    expect(lerScan(['ERRO;TAG_AUSENTE'])).toEqual({ erro: 'TAG_AUSENTE' });
  });
});
