import { Component, computed, inject, input, output, signal } from '@angular/core';
import { Esqueleto } from '../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormArray, NonNullableFormBuilder, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { data, dataHora, m3, mensagemDeErro, metros } from '../../compartilhado/utilitarios/formato';
import { ChegadaDto, PedidoDto } from '../entidades/pedido';
import { Cadastros } from '../../catalogos/servicos/cadastros';
import { Catalogos } from '../../catalogos/servicos/catalogos';
import { PreviaLaudo } from '../../laudos/componentes/previa-laudo';
import { Pedidos } from '../servicos/pedidos';
import { FORMULARIO } from '../../compartilhado/ui';
import { FormItem, ItensPedido, itensValidos, novoItemPedido, paraTabela } from './itens-pedido';
import { editavel, faltaChegar, hojeIso, situacao } from './situacao';
import { ItemParaTabela, TabelaItens } from './tabela-itens';

interface Evento { data: string; titulo: string; texto?: string; motivo?: string; tipo?: 'trava' | 'cancela'; }

/** O pedido aberto em Acompanhar pedidos: revisão, correção (até a carga chegar), cancelamento e "O que teve". */
@Component({
  selector: 'app-pedido-aberto',
  imports: [Esqueleto, FORMULARIO, ItensPedido, TabelaItens, PreviaLaudo],
  templateUrl: './pedido-aberto.html',
})
export class PedidoAberto {
  readonly id = input.required<number>();
  readonly fechar = output<void>();
  readonly mudou = output<void>();

  private readonly servico = inject(Pedidos);
  /** A carga cujo boletim (BR) está aberto na primeira visão, sem baixar. */
  protected readonly boletimDe = signal<number | null>(null);
  private readonly cadastros = inject(Cadastros);
  private readonly catalogos = inject(Catalogos);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly tamanhos = toSignal(this.catalogos.tamanhos(), { initialValue: [] });
  private readonly especies = toSignal(this.catalogos.especies(), { initialValue: [] });

  protected readonly pedido = rxResource({ params: () => this.id(), stream: ({ params }) => this.servico.buscar(params) });
  protected readonly notas = rxResource({ params: () => this.id(), stream: ({ params }) => this.servico.notas(params) });
  protected readonly chegadas = rxResource({ params: () => this.id(), stream: ({ params }) => this.servico.chegadas(params) });
  protected readonly historico = rxResource({ params: () => this.id(), stream: ({ params }) => this.servico.historico(params) });
  protected readonly contratos = rxResource({ params: () => this.pedido.value()?.idEmpresa, stream: ({ params }) => this.cadastros.contratos(params) });

  protected readonly data = data;
  protected readonly dataHora = dataHora;
  protected readonly m3 = m3;
  protected readonly situacao = situacao;
  protected readonly faltaChegar = faltaChegar;
  protected readonly editavel = editavel;
  protected readonly hoje = hojeIso();
  protected readonly volumeNota = (n: { itens: { volumeM3Declarado: number }[] }) => n.itens.reduce((a, i) => a + i.volumeM3Declarado, 0);

  protected readonly modo = signal<'ver' | 'corrigir' | 'revisar' | 'cancelar'>('ver');
  protected readonly erro = signal<string | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected readonly lote = computed(() => this.chegadas.value()?.[0]?.lote ?? null);
  protected readonly cadastradas = computed(() => (this.chegadas.value() ?? []).flatMap((c) => c.feixes).reduce((a, f) => a + f.cadastradas, 0));
  protected readonly itensTabela = computed<ItemParaTabela[]>(() =>
    (this.pedido.value()?.itens ?? []).map((i) => ({ comprimentoM: i.comprimentoM, classe: i.classe, especie: i.especie, quantidade: i.quantidade, volumeM3: i.volumeM3 })),
  );
  protected readonly eventos = computed<Evento[]>(() => {
    const p = this.pedido.value();
    if (!p) return [];
    const ev: Evento[] = [{ data: p.criadoEm, titulo: 'Pedido registrado', texto: `${p.quantidade} peças · ${m3(p.volumeM3)}` }];
    for (const h of this.historico.value() ?? []) {
      const cancelou = h.retratoDepois.includes('Cancelado');
      ev.push({ data: h.data, titulo: cancelou ? 'Pedido cancelado' : 'Pedido corrigido', motivo: h.motivo, tipo: cancelou ? 'cancela' : undefined });
    }
    for (const n of this.notas.value() ?? []) ev.push({ data: n.dataConferencia ?? n.dataEmissao, titulo: `Nota ${n.numero}`, texto: n.statusConferencia === 'Confere' ? 'confere com o pedido' : n.divergencias.join(' ') });
    (this.chegadas.value() ?? []).forEach((c, i) =>
      ev.push({ data: c.dataRecebimento, titulo: i === 0 ? 'Primeira carga chegou: o pedido travou' : 'Carga chegou', texto: `${c.quantidadeRecebida} peças em ${c.armazem} · lote ${c.lote}`, tipo: i === 0 ? 'trava' : undefined }),
    );
    return ev.sort((a, b) => a.data.localeCompare(b.data));
  });

  // correção
  protected readonly correcao = this.fb.group({
    idContrato: [null as number | null],
    dataCompra: ['', Validators.required],
    itens: this.fb.array<FormItem>([]),
    motivo: ['', Validators.required],
  });
  protected get itensCorrecao(): FormArray<FormItem> {
    return this.correcao.controls.itens;
  }
  protected readonly bloqueado = signal(false);
  protected readonly diferencas = signal<string[]>([]);
  protected readonly novosItens = signal<ItemParaTabela[]>([]);
  protected readonly motivoCancela = this.fb.control('', Validators.required);

  protected corrigir(p: PedidoDto): void {
    this.itensCorrecao.clear();
    for (const i of p.itens) this.itensCorrecao.push(novoItemPedido(this.fb, { idTamanho: i.idTamanho, classe: i.classe, idEspecie: i.idEspecie, quantidade: i.quantidade }));
    this.correcao.patchValue({ idContrato: p.idContrato, dataCompra: p.dataCompra, motivo: '' });
    this.limpar();
    this.modo.set('corrigir');
  }

  protected revisarCorrecao(p: PedidoDto): void {
    const v = this.correcao.getRawValue();
    if (this.correcao.invalid || !itensValidos(this.itensCorrecao).length || v.dataCompra > this.hoje) {
      this.correcao.markAllAsTouched();
      this.erro.set('Confira a data, os itens e escreva o motivo — ele fica no histórico do pedido.');
      return;
    }
    if (this.bloqueado()) {
      this.erro.set('A 2ª classe passou do limite: acerte os itens.');
      return;
    }
    const novos = paraTabela(itensValidos(this.itensCorrecao), this.tamanhos(), this.especies());
    const dif: string[] = [];
    if (v.dataCompra !== p.dataCompra) dif.push(`Data da compra: ${data(p.dataCompra)} → ${data(v.dataCompra)}`);
    if (v.idContrato !== p.idContrato) dif.push(`Contrato: ${p.contrato ?? '—'} → ${this.contratos.value()?.find((c) => c.id === v.idContrato)?.numeroContrato ?? '—'}`);
    const chave = (i: ItemParaTabela) => `${metros(i.comprimentoM)} · ${i.classe}ª · ${i.especie ?? 'qualquer da classe'}`;
    const antes = new Map(this.itensTabela().map((i) => [chave(i), i.quantidade]));
    const depois = new Map(novos.map((i) => [chave(i), i.quantidade]));
    for (const [k, q] of depois) if (antes.get(k) !== q) dif.push(antes.has(k) ? `${k}: ${antes.get(k)} → ${q} peças` : `Entrou ${k}: ${q} peças`);
    for (const [k, q] of antes) if (!depois.has(k)) dif.push(`Saiu ${k}: ${q} peças`);
    this.diferencas.set(dif);
    this.novosItens.set(novos);
    this.erro.set(null);
    this.modo.set('revisar');
  }

  protected salvarCorrecao(): void {
    const v = this.correcao.getRawValue();
    this.executar(
      this.servico.corrigir(this.id(), { idContrato: v.idContrato, dataCompra: v.dataCompra, itens: itensValidos(this.itensCorrecao), motivo: v.motivo.trim() }),
      'Correção salva. O motivo ficou no histórico.',
    );
  }

  protected cancelar(): void {
    if (this.motivoCancela.invalid) {
      this.motivoCancela.markAsTouched();
      this.erro.set('Escreva o motivo.');
      return;
    }
    this.executar(this.servico.cancelar(this.id(), this.motivoCancela.value.trim()), 'Pedido cancelado.');
  }

  protected abrirBoletim(c: ChegadaDto): void {
    this.boletimDe.set(this.boletimDe() === c.id ? null : c.id);
  }

  protected limpar(): void {
    this.erro.set(null);
    this.mensagem.set(null);
  }

  private executar(acao: Observable<unknown>, ok: string): void {
    this.enviando.set(true);
    this.limpar();
    acao.subscribe({
      next: () => {
        this.enviando.set(false);
        this.modo.set('ver');
        this.mensagem.set(ok);
        this.pedido.reload();
        this.historico.reload();
        this.mudou.emit();
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }
}