import { Component, computed, effect, inject, signal } from '@angular/core';
import { Esqueleto } from '../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormArray, NonNullableFormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Sessao } from '../../nucleo/sessao/sessao';
import { data, m3, mensagemDeErro, metros } from '../../compartilhado/utilitarios/formato';
import { VerificacaoPedidoDto } from '../entidades/pedido';
import { Cadastros } from '../../catalogos/servicos/cadastros';
import { Catalogos } from '../../catalogos/servicos/catalogos';
import { Pedidos } from '../servicos/pedidos';
import { FORMULARIO } from '../../compartilhado/ui';
import { FormItem, ItensPedido, itensValidos, novoItemPedido, paraTabela } from './itens-pedido';
import { hojeIso } from './situacao';
import { TabelaItens } from './tabela-itens';

const codigoNovo = () => {
  const d = new Date();
  const p = (n: number) => String(n).padStart(2, '0');
  return `PD-${d.getFullYear()}${p(d.getMonth() + 1)}${p(d.getDate())}-${p(d.getHours())}${p(d.getMinutes())}${p(d.getSeconds())}`;
};

/**
 * Cadastrar pedido (protótipo YBYRA, Pedidos → Cadastrar pedido): preenche, revisa e registra.
 * Registrado, o pedido aparece em Acompanhar pedidos, onde dá para corrigir até a carga chegar.
 */
@Component({
  selector: 'app-cadastrar-pedido',
  imports: [Esqueleto, RouterLink, FORMULARIO, ItensPedido, TabelaItens],
  templateUrl: './cadastrar-pedido.html',
})
export class CadastrarPedido {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly pedidos = inject(Pedidos);
  private readonly cadastros = inject(Cadastros);
  private readonly catalogos = inject(Catalogos);
  private readonly router = inject(Router);
  protected readonly usuario = inject(Sessao).usuario;
  protected readonly data = data;
  protected readonly metros = metros;
  protected readonly m3 = m3;
  protected readonly hoje = hojeIso();

  protected readonly fornecedores = rxResource({ stream: () => this.cadastros.empresas('Fornecedor') });
  private readonly tamanhos = toSignal(this.catalogos.tamanhos(), { initialValue: [] });
  private readonly especies = toSignal(this.catalogos.especies(), { initialValue: [] });

  protected readonly form = this.fb.group({
    codigo: [codigoNovo(), Validators.required],
    idEmpresa: [0, Validators.min(1)],
    idContrato: [null as number | null],
    dataCompra: [this.hoje, Validators.required],
    itens: this.fb.array<FormItem>([novoItemPedido(this.fb)]),
  });
  protected get itens(): FormArray<FormItem> {
    return this.form.controls.itens;
  }
  protected readonly idEmpresa = toSignal(this.form.controls.idEmpresa.valueChanges, { initialValue: 0 });
  protected readonly contratos = rxResource({ params: () => this.idEmpresa() || undefined, stream: ({ params }) => this.cadastros.contratos(params) });
  protected readonly ofertas = rxResource({ params: () => this.idEmpresa() || undefined, stream: ({ params }) => this.pedidos.ofertas(params) });
  protected readonly contrato = computed(() => this.contratos.value()?.find((c) => c.id === this.idContrato()) ?? null);
  private readonly idContrato = signal<number | null>(null);
  protected readonly fornecedor = computed(() => this.fornecedores.value()?.find((f) => f.id === this.idEmpresa()) ?? null);

  protected readonly passo = signal<'preencher' | 'revisar'>('preencher');
  protected readonly verificacao = signal<VerificacaoPedidoDto | null>(null);
  protected readonly revisao = signal<ReturnType<typeof paraTabela>>([]);
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  constructor() {
    // o contrato sai do fornecedor: escolhe o primeiro sozinho
    this.form.controls.idEmpresa.valueChanges.subscribe(() => this.form.controls.idContrato.setValue(null));
    this.form.controls.idContrato.valueChanges.subscribe((v) => this.idContrato.set(v));
    effect(() => {
      const lista = this.contratos.value() ?? [];
      if (lista.length && this.form.controls.idContrato.value === null) this.form.controls.idContrato.setValue(lista[0].id);
    });
  }

  protected revisar(): void {
    const dataOk = this.form.controls.dataCompra.value && this.form.controls.dataCompra.value <= this.hoje;
    if (this.form.invalid || !dataOk || !itensValidos(this.itens).length) {
      this.form.markAllAsTouched();
      this.erro.set(!dataOk ? 'Informe a data da compra — não pode ser futura.' : 'Escolha o fornecedor e pelo menos um item com tamanho e peças.');
      return;
    }
    if (this.verificacao()?.segundaClasse.valido === false) {
      this.erro.set('A 2ª classe passou do limite: acerte os itens antes de revisar.');
      return;
    }
    this.erro.set(null);
    this.revisao.set(paraTabela(itensValidos(this.itens), this.tamanhos(), this.especies()));
    this.passo.set('revisar');
  }

  protected registrar(): void {
    const v = this.form.getRawValue();
    this.enviando.set(true);
    this.erro.set(null);
    this.pedidos
      .criar({ codigo: v.codigo.trim(), idEmpresa: v.idEmpresa, idContrato: v.idContrato, dataCompra: v.dataCompra, itens: itensValidos(this.itens) })
      .subscribe({
        next: (p) => this.router.navigate(['/pedidos'], { queryParams: { aberto: p.id, registrado: 1 } }),
        error: (e) => {
          this.erro.set(mensagemDeErro(e));
          this.enviando.set(false);
        },
      });
  }
}