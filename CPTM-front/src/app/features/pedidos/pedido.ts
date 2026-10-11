import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { abrirArquivo } from '../../helpers/arquivo';
import { data, dataHora, m3, mensagemDeErro, metros } from '../../helpers/formato';
import { ChegadaDto, NotaDto, NovaNotaItemDto, PedidoItemDto } from '../../models/pedido';
import { Catalogos } from '../../services/catalogos';
import { Laudos } from '../../services/laudos';
import { Pedidos } from '../../services/pedidos';
import { Estado } from '../../shared/estado/estado';
import { Coluna, Tabela } from '../../shared/tabela/tabela';
import { FORMULARIO } from '../../shared/ui';

/** O pedido aberto: itens, notas (conferidas na hora), chegadas com o boletim, cancelamento e histórico. */
@Component({
  selector: 'app-pedido',
  imports: [RouterLink, Estado, Tabela, FORMULARIO],
  templateUrl: './pedido.html',
})
export class Pedido {
  readonly id = input.required<string>();
  private readonly servico = inject(Pedidos);
  private readonly laudos = inject(Laudos);
  private readonly fb = inject(FormBuilder).nonNullable;

  private readonly idNumero = computed(() => Number(this.id()));
  protected readonly pedido = rxResource({ params: () => this.idNumero(), stream: ({ params }) => this.servico.buscar(params) });
  protected readonly notas = rxResource({ params: () => this.idNumero(), stream: ({ params }) => this.servico.notas(params) });
  protected readonly chegadas = rxResource({ params: () => this.idNumero(), stream: ({ params }) => this.servico.chegadas(params) });
  protected readonly historico = rxResource({ params: () => this.idNumero(), stream: ({ params }) => this.servico.historico(params) });
  protected readonly erro = computed(() => (this.pedido.error() ? mensagemDeErro(this.pedido.error()) : null));
  protected readonly podeMexer = computed(() => this.pedido.value()?.situacao !== 'Cancelado' && !(this.chegadas.value()?.length));

  protected readonly tamanhos = toSignal(inject(Catalogos).tamanhos(), { initialValue: [] });
  protected readonly especies = toSignal(inject(Catalogos).especies(), { initialValue: [] });
  protected readonly metros = metros;
  protected readonly dataHora = dataHora;
  protected readonly m3 = m3;

  protected readonly colunasItens: Coluna<PedidoItemDto>[] = [
    { titulo: 'Tamanho', valor: (i) => metros(i.comprimentoM) },
    { titulo: 'Classe', valor: (i) => `${i.classe}ª` },
    { titulo: 'Espécie', valor: (i) => i.especie ?? 'Qualquer da classe' },
    { titulo: 'Quantidade', valor: (i) => i.quantidade },
    { titulo: 'Volume', valor: (i) => m3(i.volumeM3) },
  ];
  protected readonly colunasNotas: Coluna<NotaDto>[] = [
    { titulo: 'Nota', valor: (n) => n.numero, mono: true },
    { titulo: 'Emissão', valor: (n) => data(n.dataEmissao) },
    { titulo: 'Conferência', valor: (n) => n.statusConferencia, vermelho: (n) => n.statusConferencia === 'Diverge' },
    { titulo: 'Divergências', valor: (n) => n.divergencias.join(' ') || '—', vermelho: (n) => n.divergencias.length > 0 },
    { titulo: 'Avisos', valor: (n) => n.avisos.join(' ') || '—' },
  ];
  protected readonly colunasChegadas: Coluna<ChegadaDto>[] = [
    { titulo: 'Recebida em', valor: (c) => dataHora(c.dataRecebimento) },
    { titulo: 'Armazém', valor: (c) => c.armazem },
    { titulo: 'Nota', valor: (c) => c.numeroNota, mono: true },
    { titulo: 'Peças', valor: (c) => c.quantidadeRecebida },
    { titulo: 'Lote', valor: (c) => c.lote, mono: true },
    { titulo: 'Conferência', valor: (c) => (c.confere ? 'Confere' : c.divergencias.join(' ')), vermelho: (c) => !c.confere },
  ];

  protected readonly nota = this.fb.group({
    numero: ['', Validators.required],
    dataEmissao: [''],
    valor: [null as number | null],
    itens: this.fb.array([this.novoItemNota()]),
  });
  protected readonly motivo = this.fb.control('', Validators.required);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erroAcao = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected novoItemNota() {
    return this.fb.group({
      idEspecie: [0, Validators.min(1)],
      idTamanho: [0, Validators.min(1)],
      classe: [1],
      quantidade: [1, Validators.min(1)],
      volumeM3: [0, Validators.min(0)],
    });
  }

  protected especieDaNota(i: number, idEspecie: number): void {
    const especie = this.especies().find((e) => e.id === idEspecie);
    if (especie) this.nota.controls.itens.at(i).patchValue({ classe: especie.classe });
  }

  protected registrarNota(): void {
    if (this.nota.invalid) {
      this.nota.markAllAsTouched();
      return;
    }
    const v = this.nota.getRawValue();
    this.executar(
      this.servico.novaNota(this.idNumero(), { numero: v.numero.trim(), dataEmissao: v.dataEmissao || null, valor: v.valor, itens: v.itens as NovaNotaItemDto[] }),
      (n) => {
        this.nota.reset();
        this.notas.reload();
        return n.statusConferencia === 'Confere' ? `Nota ${n.numero} confere com o pedido.` : `Nota ${n.numero} registrada: ${n.divergencias.join(' ')}`;
      },
    );
  }

  protected cancelar(): void {
    if (this.motivo.invalid) {
      this.motivo.markAsTouched();
      return;
    }
    this.executar(this.servico.cancelar(this.idNumero(), this.motivo.value.trim()), () => {
      this.pedido.reload();
      this.historico.reload();
      return 'Pedido cancelado.';
    });
  }

  protected abrirBoletim(c: ChegadaDto): void {
    this.laudos.pdf('recebimento', c.id).subscribe({
      next: (pdf) => abrirArquivo(pdf, `BR-${c.id}.pdf`),
      error: (e) => this.erroAcao.set(mensagemDeErro(e)),
    });
  }

  private executar<T>(acao: import('rxjs').Observable<T>, depois: (r: T) => string): void {
    this.enviando.set(true);
    this.erroAcao.set(null);
    this.mensagem.set(null);
    acao.subscribe({
      next: (r) => {
        this.mensagem.set(depois(r));
        this.enviando.set(false);
      },
      error: (e) => {
        this.erroAcao.set(mensagemDeErro(e));
        this.enviando.set(false);
      },
    });
  }
}