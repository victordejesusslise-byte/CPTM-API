import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FormArray, NonNullableFormBuilder } from '@angular/forms';
import { MedidaDto } from '../../catalogos/entidades/cadastro';
import { FormItem, ItensPedido, novoItemPedido, padroesDoItem } from './itens-pedido';

const medida = (id: number, idEspecie: number | null, ativo = true): MedidaDto =>
  ({ id, idEmpresa: 1, idEspecie, especie: idEspecie ? 'Apuleia molaris' : null, larguraM: 0.22, alturaM: 0.18, ativo, rotulo: '0,22 × 0,18 m' });
// como o TG no banco de teste: os dois padrões de todas as espécies desativados, e um ativo só do Amarelão (espécie 5)
const medidas = [medida(1, null, false), medida(2, null, false), medida(3, 5)];

describe('ItensPedido (Victor, 2026-10-31: "não consigo revisar o pedido")', () => {
  it('com a espécie em branco, o padrão lista todos os ativos; com espécie, só os que servem a ela', () => {
    expect(padroesDoItem(medidas, null).map((m) => m.id)).toEqual([3]);
    expect(padroesDoItem(medidas, 5).map((m) => m.id)).toEqual([3]);
    expect(padroesDoItem(medidas, 9).map((m) => m.id)).toEqual([]);
    expect(padroesDoItem([medida(7, null), medida(3, 5)], 9).map((m) => m.id)).toEqual([7]);
  });

  it('escolher o padrão de uma espécie com a espécie em branco preenche a espécie; limpar a espécie depois tira o padrão', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const fb = TestBed.inject(NonNullableFormBuilder);
    const itens = new FormArray<FormItem>([novoItemPedido(fb, { idTamanho: 1 })]);
    const fixture = TestBed.createComponent(ItensPedido);
    fixture.componentRef.setInput('itens', itens);
    fixture.componentRef.setInput('idEmpresa', 1);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.match((r) => r.url.includes('empresas')).forEach((r) => r.flush([{ id: 1, nome: 'TG–Três Guri', medidas }]));
    http.match(() => true).forEach((r) => r.flush([]));
    await fixture.whenStable();
    const item = itens.at(0);
    const esperar = () => new Promise((r) => setTimeout(r, 400));

    item.controls.idMedida.setValue(3);
    await esperar();
    expect(item.controls.idEspecie.value).toBe(5);

    item.controls.idEspecie.setValue(null);
    await esperar();
    expect(item.controls.idMedida.value).toBe(0);
  });
});
