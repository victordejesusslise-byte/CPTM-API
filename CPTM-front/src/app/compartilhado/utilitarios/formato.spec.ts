import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';
import { consulta } from './consulta';
import { data, diasDesde, gps, km, m3, mensagemDeErro, nomeDoCargo } from './formato';

describe('formato', () => {
  it('mostra o GPS com 5 casas (cerca de 1 m), ou o traço quando falta', () => {
    expect(gps(-23.5371, -46.7)).toBe('-23,53710 · -46,70000');
    expect(gps(null, -46.7)).toBe('—');
    expect(gps(-23.5371, undefined)).toBe('—');
  });

  it('a data só com o dia mostra o mesmo dia, em qualquer fuso (a compra de 31/10 não vira 30/10)', () => {
    expect(data('2026-10-31')).toBe('31/10/2026');
    expect(data('2026-01-01')).toBe('01/01/2026');
    expect(data(null)).toBe('—');
  });

  it('formata m³ e km como no Brasil', () => {
    expect(m3(3.19872)).toBe('3,199 m³');
    expect(km(213.12)).toBe('213,120');
    expect(m3(null)).toBe('—');
  });

  it('mostra o nome do cargo', () => {
    expect(nomeDoCargo('OperadorPatio')).toBe('Operador de pátio');
    expect(nomeDoCargo('Tecnico')).toBe('Técnico');
  });

  it('usa a mensagem da API quando ela manda', () => {
    const e = new HttpErrorResponse({ status: 400, error: { title: 'Regra', detail: 'A 2ª classe passou do limite.' } });
    expect(mensagemDeErro(e)).toBe('A 2ª classe passou do limite.');
    expect(mensagemDeErro(new HttpErrorResponse({ status: 0 }))).toBe('Sem conexão com o servidor.');
    // O pedido que passou do tempo limite (o celular sem rede ou sem o Tailscale): diz o que conferir.
    expect(mensagemDeErro(new TimeoutError())).toBe('Sem resposta do servidor: confira a internet e o Tailscale e tente de novo.');
    expect(mensagemDeErro(new HttpErrorResponse({ status: 403 }))).toBe('Seu cargo não pode fazer isso.');
  });

  it('monta a consulta sem os campos vazios', () => {
    expect(consulta({ idEmpresa: 3, papel: null, texto: '', ativo: false }).toString()).toBe('idEmpresa=3&ativo=false');
  });

  it('conta os dias inteiros e nunca mostra dia negativo', () => {
    const agora = new Date('2026-10-13T18:00:00').getTime();
    expect(diasDesde('2026-10-11T17:00:00', agora)).toBe(2);
    expect(diasDesde('2026-10-13T21:00:00', agora)).toBe(0);
    expect(diasDesde(null, agora)).toBeNull();
  });
});
