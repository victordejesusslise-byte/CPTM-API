import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ambiente } from '../../../environments/ambiente';
import { entrarComo } from '../../compartilhado/testes/sessao-de-teste';
import { Fila, NovoRegistro } from './fila';
import { GUARDADO } from './guardado';
import { Rede } from './rede';

describe('Fila (sem internet)', () => {
  const online = signal(true);
  let http: HttpTestingController;
  const foto = `${ambiente.api}/trocas/7/foto`;
  const execucao = `${ambiente.api}/trocas/7/execucao`;
  const troca: NovoRegistro = { tipo: 'troca', alvo: 7, corpo: { equipe: 'Turma 2' }, rotulo: 'Troca da peça DM-1' };
  const imagem = () => new Blob(['foto'], { type: 'image/jpeg' });

  const preparar = () => {
    localStorage.clear();
    entrarComo('OperadorVia');
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Rede, useValue: { online } }] });
    http = TestBed.inject(HttpTestingController);
  };
  const abrir = () => {
    preparar();
    return TestBed.inject(Fila);
  };
  /** Espera a fila fazer a chamada (ela é assíncrona: guarda a foto antes). */
  const pedido = (url: string): Promise<TestRequest> =>
    vi.waitFor(() => {
      const achados = http.match(url);
      if (!achados.length) throw new Error('esperando ' + url);
      return achados[0];
    });
  const recusa = { title: 'Regra do sistema', detail: 'Só a troca com a peça separada é feita.' };

  beforeEach(() => online.set(true));

  it('com rede, sobe a foto e depois o registro com o caminho dela, o ID do celular e a hora do campo', async () => {
    const fila = abrir();
    const r = fila.registrar(troca, imagem());
    (await pedido(foto)).flush({ arquivo: 'trocas/7/a.jpg' });
    const registro = await pedido(execucao);
    expect(registro.request.body).toMatchObject({ equipe: 'Turma 2', fotoArquivo: 'trocas/7/a.jpg' });
    expect(registro.request.body.idCliente).toMatch(/^[0-9a-f-]{36}$/);
    expect(registro.request.body.registradoEm).toBeTruthy();
    registro.flush({ id: 7, status: 'Feita' });
    expect(await r).toEqual({ estado: 'enviado', resposta: { id: 7, status: 'Feita' } });
    expect(fila.itens()).toEqual([]);
  });

  it('sem rede, guarda; quando a rede volta, sobe sozinha', async () => {
    online.set(false);
    const fila = abrir();
    expect(await fila.registrar(troca, imagem())).toEqual({ estado: 'guardado' });
    expect(fila.meus()).toHaveLength(1);
    http.expectNone(foto);

    online.set(true);
    TestBed.tick();
    (await pedido(foto)).flush({ arquivo: 'trocas/7/a.jpg' });
    (await pedido(execucao)).flush({ id: 7 });
    await vi.waitFor(() => expect(fila.itens()).toEqual([]));
  });

  it('com o servidor fora, fica na fila; no reenvio a foto não sobe de novo e o ID é o mesmo', async () => {
    const fila = abrir();
    const r = fila.registrar(troca, imagem());
    (await pedido(foto)).flush({ arquivo: 'trocas/7/a.jpg' });
    const primeiro = await pedido(execucao);
    const id = primeiro.request.body.idCliente;
    primeiro.flush('fora', { status: 503, statusText: 'Fora' });
    expect(await r).toEqual({ estado: 'guardado' });
    expect(fila.itens()[0]).toMatchObject({ tentativas: 1, arquivo: 'trocas/7/a.jpg' });

    const deNovo = fila.enviarTudo();
    const segundo = await pedido(execucao);
    http.expectNone(foto);
    expect(segundo.request.body.idCliente).toBe(id);
    segundo.flush({ id: 7 });
    await deNovo;
    expect(fila.itens()).toEqual([]);
  });

  it('recusado na hora: a tela recebe o motivo e nada fica guardado', async () => {
    const fila = abrir();
    const r = fila.registrar(troca, imagem());
    (await pedido(foto)).flush({ arquivo: 'trocas/7/a.jpg' });
    (await pedido(execucao)).flush(recusa, { status: 400, statusText: 'Bad Request' });
    expect(await r).toEqual({ estado: 'recusado', motivo: 'Só a troca com a peça separada é feita.' });
    expect(fila.itens()).toEqual([]);
    expect(fila.naoEnviados()).toEqual([]);
  });

  it('recusado depois, já sem a pessoa na tela: vai para Não enviados, com o motivo', async () => {
    online.set(false);
    const fila = abrir();
    await fila.registrar(troca, imagem());
    online.set(true);
    TestBed.tick();
    (await pedido(foto)).flush({ arquivo: 'trocas/7/a.jpg' });
    (await pedido(execucao)).flush(recusa, { status: 400, statusText: 'Bad Request' });
    await vi.waitFor(() => expect(fila.naoEnviados()).toHaveLength(1));
    expect(fila.naoEnviados()[0]).toMatchObject({ rotulo: 'Troca da peça DM-1', motivo: 'Só a troca com a peça separada é feita.' });
    await fila.descartar(fila.naoEnviados()[0]);
    expect(fila.naoEnviados()).toEqual([]);
  });

  it('só sobem os registros de quem está logado', async () => {
    preparar();
    const outro = { ...troca, id: 'outro', idUsuario: 99, criadoEm: new Date().toISOString(), foto: 'outro.jpg', tentativas: 0 };
    await TestBed.inject(GUARDADO).gravar('ybyra.fila', JSON.stringify({ itens: [outro], naoEnviados: [] }));
    const fila = TestBed.inject(Fila);
    await fila.enviarTudo();
    expect(fila.itens()).toHaveLength(1);
    expect(fila.meus()).toEqual([]);
    http.expectNone(foto);
  });
});
