import { HttpErrorResponse } from '@angular/common/http';
import { consulta } from './consulta';
import { diasDesde, km, m3, mensagemDeErro, nomeDoCargo } from './formato';

describe('formato', () => {
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
