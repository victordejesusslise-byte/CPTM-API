import { Component, computed, inject, input, signal } from '@angular/core';
import { TIPOS_LEITOR } from '../../entidades/catalogo';
import { EmpresaDto } from '../../entidades/cadastro';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { data, dataHora, km, mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { PapelEmpresa } from '../../../compartilhado/entidades/enums';
import { Cadastros as CadastrosApi } from '../../servicos/cadastros';
import { Catalogos } from '../../servicos/catalogos';
import { FORMULARIO } from '../../../compartilhado/ui';

/** Um padrão de medida no formulário (largura e altura em metros; sem espécie, vale para todas). */
const novoPadrao = () => ({ idEspecie: null as number | null, larguraM: null as number | null, alturaM: null as number | null });
const padraoValido = (p: { larguraM: number | null; alturaM: number | null }) =>
  p.larguraM !== null && p.alturaM !== null && p.larguraM >= 0.05 && p.larguraM <= 1 && p.alturaM >= 0.05 && p.alturaM <= 1;

type Aba = 'empresas' | 'contratos' | 'via' | 'parametros' | 'leitores';
const TITULOS: Record<Aba, string> = { empresas: 'Empresas', contratos: 'Contratos', via: 'Estações e trechos', parametros: 'Parâmetros', leitores: 'Leitores de tag' };

/** Cadastros de apoio (supervisor): empresas, contratos, estações e trechos, parâmetros do sistema. */
@Component({
  selector: 'app-cadastros',
  imports: [Esqueleto, FormsModule, FORMULARIO],
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
  private readonly catalogos = inject(Catalogos);
  protected readonly leitores = rxResource({ params: () => (this.aba() === 'leitores' ? true : undefined), stream: () => this.catalogos.leitores() });
  protected readonly leitor = { idTipoLeitor: 1, codigo: '', numeroSerie: '' };
  protected readonly tiposLeitor = TIPOS_LEITOR;
  protected readonly fornecedores = computed(() => (this.empresas.value() ?? []).filter((e) => e.papel === 'Fornecedor'));
  protected readonly data = data;
  protected readonly dataHora = dataHora;
  protected readonly km = km;
  protected readonly papeis: PapelEmpresa[] = ['Fornecedor', 'Tratadora', 'Inspetora'];

  // formulários (um objeto por aba)
  protected readonly empresa = { nome: '', cnpj: '', papel: 'Fornecedor' as PapelEmpresa, medidas: [novoPadrao()] };
  /** O fornecedor com os padrões de medida abertos na tabela. */
  protected readonly padroesDe = signal<number | null>(null);
  protected readonly padrao = novoPadrao();
  protected readonly especies = toSignal(inject(Catalogos).especies(), { initialValue: [] });
  protected readonly ativos = (e: EmpresaDto) => e.medidas.filter((m) => m.ativo).length;
  protected readonly contrato = { idEmpresa: 0, numeroAta: '', numeroContrato: '', vigenciaInicio: '', vigenciaFim: '' };
  protected readonly estacao = { nome: '', linha: '9-Esmeralda', km: null as number | null, ordem: null as number | null };
  protected readonly trecho = { idEstacaoInicio: 0, idEstacaoFim: 0, classeExigida: 1 };
  protected readonly valores: Record<string, string> = {};

  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected salvarEmpresa(): void {
    if (!this.empresa.nome.trim()) return this.erro.set('Informe o nome da empresa.');
    const fornecedor = this.empresa.papel === 'Fornecedor';
    if (fornecedor && !this.empresa.medidas.every(padraoValido)) return this.erro.set('Informe a largura e a altura de cada padrão de medida, de 0,05 a 1 m.');
    const medidas = fornecedor ? this.empresa.medidas.map((m) => ({ idEspecie: m.idEspecie, larguraM: m.larguraM!, alturaM: m.alturaM! })) : [];
    this.enviar(this.api.novaEmpresa({ nome: this.empresa.nome.trim(), cnpj: this.empresa.cnpj.trim() || null, papel: this.empresa.papel, medidas }), 'Empresa cadastrada.', () => {
      this.empresa.nome = '';
      this.empresa.cnpj = '';
      this.empresa.medidas = [novoPadrao()];
      this.empresas.reload();
    });
  }

  protected adicionarPadrao(): void {
    this.empresa.medidas.push(novoPadrao());
  }

  protected tirarPadrao(i: number): void {
    if (this.empresa.medidas.length > 1) this.empresa.medidas.splice(i, 1);
  }

  protected abrirPadroes(id: number): void {
    this.padroesDe.update((atual) => (atual === id ? null : id));
    Object.assign(this.padrao, novoPadrao());
  }

  protected salvarPadrao(idEmpresa: number): void {
    const p = this.padrao;
    if (!padraoValido(p)) return this.erro.set('Informe a largura e a altura do padrão, de 0,05 a 1 m.');
    this.enviar(this.api.novaMedida(idEmpresa, { idEspecie: p.idEspecie, larguraM: p.larguraM!, alturaM: p.alturaM! }), 'Padrão de medida cadastrado.', () => {
      Object.assign(p, novoPadrao());
      this.empresas.reload();
    });
  }

  protected ativarPadrao(id: number, ativo: boolean): void {
    this.enviar(this.api.ativarMedida(id, ativo), ativo ? 'Padrão ativado.' : 'Padrão desativado: ele some da escolha do pedido.', () => this.empresas.reload());
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

  protected salvarLeitor(): void {
    const l = this.leitor;
    if (!l.codigo.trim()) return this.erro.set('Informe o código do leitor.');
    this.enviar(this.catalogos.novoLeitor({ idTipoLeitor: l.idTipoLeitor, codigo: l.codigo.trim(), numeroSerie: l.numeroSerie.trim() || null }), 'Leitor cadastrado.', () => {
      l.codigo = '';
      l.numeroSerie = '';
      this.leitores.reload();
    });
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