import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { dataHora, km, mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { PassagemDto, Passagens } from '../../servicos/passagens';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { FORMULARIO } from '../../../compartilhado/ui';
import { Nota } from '../../../compartilhado/componentes/nota/nota';

type Aba = 'noite' | 'anteriores' | 'numeros';
const TITULOS: Record<Aba, string> = { noite: 'Passagem da noite', anteriores: 'Passagens anteriores', numeros: 'Números do período' };

/**
 * Passagem (protótipo YBYRA, etapa 08): o que o carro leu de noite — a tag de cada peça, a posição (desvio) e, junto,
 * o que a câmera com a IA apontou. A da noite abre direto; as anteriores, pela lista; os números somam o período.
 */
@Component({
  selector: 'app-passagem',
  imports: [FORMULARIO, Estado, Nota],
  templateUrl: './passagem.html',
})
export class Passagem {
  readonly aba = input<Aba>('noite');
  private readonly api = inject(Passagens);
  protected readonly titulo = computed(() => TITULOS[this.aba()]);
  protected readonly lista = rxResource({ stream: () => this.api.ultimas(30) });
  protected readonly erro = computed(() => (this.lista.error() ? mensagemDeErro(this.lista.error()) : null));
  protected readonly escolhida = signal<number | null>(null);
  protected readonly idAberta = computed(() => (this.aba() === 'numeros' ? null : (this.escolhida() ?? (this.aba() === 'noite' ? (this.lista.value()?.[0]?.id ?? null) : null))));
  protected readonly detalhe = rxResource({ params: () => this.idAberta() ?? undefined, stream: ({ params }) => (params ? this.api.buscar(params) : of(null)) });
  protected readonly so = signal<'problemas' | 'todas'>('problemas');
  protected readonly leituras = computed(() => {
    const l = this.detalhe.value()?.leituras ?? [];
    return this.so() === 'todas' ? l : l.filter((x) => !x.leitura || x.foraDePosicao || x.rachaduraSuspeita || x.idDormente === null);
  });
  protected readonly soma = computed(() => {
    const l = this.lista.value() ?? [];
    const leituras = l.reduce((a, p) => a + p.leituras, 0);
    return {
      passagens: l.length, leituras,
      semResposta: l.reduce((a, p) => a + p.semResposta, 0),
      fora: l.reduce((a, p) => a + p.foraDePosicao, 0),
      desconhecidas: l.reduce((a, p) => a + p.tagsDesconhecidas, 0),
      alertas: l.reduce((a, p) => a + p.alertasSensor + p.alertasImagem, 0),
      imagem: l.reduce((a, p) => a + p.alertasImagem, 0),
      resposta: leituras ? Math.round(((leituras - l.reduce((a, p) => a + p.semResposta, 0)) / leituras) * 1000) / 10 : null,
    };
  });
  protected readonly dataHora = dataHora;
  protected readonly km = km;

  protected abrir(p: PassagemDto): void {
    this.escolhida.set(p.id);
    this.so.set('problemas');
  }
}
