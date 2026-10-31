import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { provideRouter } from '@angular/router';
import { entrarComo } from '../../../compartilhado/testes/sessao-de-teste';
import { Qualidade, defeitoDaVerificacao } from './qualidade';

describe('Qualidade (Victor, 2026-10-31: com ataque biológico ou rachadura, reprova sozinho, para evitar erro humano)', () => {
  it('diz o defeito; sem defeito, nada', () => {
    expect(defeitoDaVerificacao('Nenhum', 'Nenhuma')).toBeNull();
    expect(defeitoDaVerificacao('Suspeito', 'Nenhuma')).toBe('Ataque biológico suspeito');
    expect(defeitoDaVerificacao('Presente', 'Grave')).toBe('Ataque biológico presente; rachadura grave');
    expect(defeitoDaVerificacao('Nenhum', 'Leve')).toBe('Rachadura leve');
  });

  it('marcar o defeito reprova, escreve o motivo e trava o Aprovada; tirar o defeito volta', async () => {
    localStorage.clear();
    entrarComo('Especialista');
    await TestBed.configureTestingModule({ imports: [Qualidade], providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    // o formulário e o sinal são protegidos (da tela): o teste mexe neles como o especialista mexeria nos campos
    const tela = TestBed.createComponent(Qualidade).componentInstance as unknown as { form: FormGroup; temDefeito: () => boolean };
    const controls = tela.form.controls;

    controls['rachadura'].setValue('Leve');
    expect(tela.form.getRawValue()).toMatchObject({ resultado: 'Reprovada', motivo: 'Rachadura leve' });
    expect(tela.temDefeito()).toBe(true);
    controls['ataqueBiologico'].setValue('Suspeito');
    expect(controls['motivo'].value).toBe('Ataque biológico suspeito; rachadura leve');

    controls['ataqueBiologico'].setValue('Nenhum');
    controls['rachadura'].setValue('Nenhuma');
    expect(tela.form.getRawValue()).toMatchObject({ resultado: 'Aprovada', motivo: '' });
    expect(tela.temDefeito()).toBe(false);

    // o motivo que o especialista escreveu não é apagado
    controls['rachadura'].setValue('Grave');
    controls['motivo'].setValue('Rachadura grave no topo, perto da placa');
    controls['ataqueBiologico'].setValue('Presente');
    expect(controls['motivo'].value).toBe('Rachadura grave no topo, perto da placa');
    expect(controls['resultado'].value).toBe('Reprovada');
  });
});
