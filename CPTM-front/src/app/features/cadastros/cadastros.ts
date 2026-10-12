import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { data, dataHora, km, mensagemDeErro } from '../../helpers/formato';
import { PapelEmpresa } from '../../models/enums';
import { Cadastros as CadastrosApi } from '../../services/cadastros';
import { FORMULARIO } from '../../shared/ui';

type Aba = 'empresas' | 'contratos' | 'via' | 'parametros';
const TITULOS: Record<Aba, string> = { empresas: 'Empresas', contratos: 'Contratos', via: 'Estações e trechos', parametros: 'Parâmetros' };

/** Cadastros de apoio (supervisor): empresas, contratos, estações e trechos, parâmetros do sistema. */
@Component({
  selector: 'app-cadastros',
  imports: [FormsModule, FORMULARIO],
  templateUrl: './cadastros.html',
})
export class Cadastros {
  readonly aba = input<Aba>('empresas');
  private readonly api = inject(CadastrosApi);
  protected readonly titulo = computed(() => TITULOS[this.aba()]);
  protected readonly empresas = rxResource({ stream: () => this.api.empresas() });
  protected readonly contratos = rxResource({ stream: () => this.api.contratos() });
  protected readonly estacoes = rxResource({ stream: () => this.api.estacoes() });
  protected readonly trechos = rxResource({ stream: () => this.api.trechos() });
  protected readonly parametros = rxResource({ stream: () => this.api.parametros() });
  protected readonly fornecedores = computed(() => (this.empresas.value() ?? []).filter((e) => e.papel === 'Fornecedor'));
  protected readonly data = data;
  protected readonly dataHora = dataHora;
  protected readonly km = km;
  protected readonly papeis: PapelEmpresa[] = ['Fornecedor', 'Tratadora', 'Inspetora'];

  // formulários (um objeto por aba)
  protected readonly empresa = { nome: '', cnpj: '', papel: 'Fornecedor' as PapelEmpresa };
  protected readonly contrato = { idEmpresa: 0, numeroAta: '', numeroContrato: '', vigenciaInicio: '', vigenciaFim: '' };
  protected readonly estacao = { nome: '', linha: '9-Esmeralda', km: null as number | null, ordem: null as number | null };
  protected readonly trecho = { idEstacaoInicio: 0, idEstacaoFim: 0, classeExigida: 1 };
  protected readonly valores: Record<string, string> = {};

  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected salvarEmpresa(): void {
    if (!this.empresa.nome.trim()) return this.erro.set('Informe o nome da empresa.');
    this.enviar(this.api.novaEmpresa({ nome: this.empresa.nome.trim(), cnpj: this.empresa.cnpj.trim() || null, papel: this.empresa.papel }), 'Empresa cadastrada.', () => {
      this.empresa.nome = '';
      this.empresa.cnpj = '';
      this.empresas.reload();
    });
  }

  protected ativar(id: number, ativo: boolean): void {
    this.enviar(this.api.ativarEmpresa(id, ativo), ativo ? 'Empresa ativada.' : 'Empresa desativada: ela some das listas de escolha.', () => this.empresas.reload());
  }

  protected salvarContrato(): void {
    const c = this.contrato;
    if (!c.idEmpresa || !c.numeroAta.trim() || !c.numeroContrato.trim()) return this.erro.set('Escolha o fornecedor e informe a ata e o contrato.');
    this.enviar(
      this.api.novoContrato({ idEmpresa: c.idEmpresa, numeroAta: c.numeroAta.trim(), numeroContrato: c.numeroContrato.trim(), vigenciaInicio: c.vigenciaInicio || null, vigenciaFim: c.vigenciaFim || null }),
      'Contrato cadastrado.',
      () => {
        c.numeroAta = '';
        c.numeroContrato = '';
        this.contratos.reload();
      },
    );
  }

  protected salvarEstacao(): void {
    const e = this.estacao;
    if (!e.nome.trim() || e.km === null || e.ordem === null) return this.erro.set('Informe o nome, o km e a ordem da estação.');
    this.enviar(this.api.novaEstacao({ nome: e.nome.trim(), linha: e.linha, km: e.km, ordem: e.ordem }), 'Estação cadastrada.', () => {
      e.nome = '';
      e.km = null;
      e.ordem = null;
      this.estacoes.reload();
    });
  }

  protected salvarTrecho(): void {
    const t = this.trecho;
    if (!t.idEstacaoInicio || !t.idEstacaoFim || t.idEstacaoInicio === t.idEstacaoFim) return this.erro.set('Escolha duas estações diferentes.');
    this.enviar(this.api.novoTrecho({ ...t }), 'Trecho cadastrado.', () => this.trechos.reload());
  }

  protected salvarParametro(chave: string, atual: string): void {
    const valor = (this.valores[chave] ?? atual).trim();
    if (!valor || valor === atual) return;
    this.enviar(this.api.alterarParametro(chave, valor), `Parâmetro ${chave} alterado.`, () => this.parametros.reload());
  }

  private enviar(acao: import('rxjs').Observable<unknown>, ok: string, depois: () => void): void {
    this.enviando.set(true);
    this.erro.set(null);
    this.mensagem.set(null);
    acao.subscribe({
      next: () => {
        this.enviando.set(false);
        this.mensagem.set(ok);
        depois();
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }
}