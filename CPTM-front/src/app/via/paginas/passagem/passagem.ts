import { Component, computed, inject, input, signal } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { dataHora, km, mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { PassagemDetalheDto, PassagemDto, Passagens } from '../../servicos/passagens';
import { TrabalhoDaNoite } from '../../../nucleo/ao-vivo/trabalho-da-noite';
import { AvisoGuardado } from '../../../nucleo/ao-vivo/aviso-guardado';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { FORMULARIO } from '../../../compartilhado/ui';
import { Nota } from '../../../compartilhado/componentes/nota/nota';
import { CartoesNoCelular } from '../../../compartilhado/componentes/tabela/cartoes';
import { valorDe } from '../../../compartilhado/utilitarios/recurso';

type Aba = 'noite' | 'anteriores' | 'numeros';
const TITULOS: Record<Aba, string> = { noite: 'Passagem da noite', anteriores: 'Passagens anteriores', numeros: 'Números do período' };

/**
 * Passagem (protótipo YBYRA, etapa 08): o que o carro leu de noite — a tag de cada peça, a posição (desvio) e, junto,
 * o que a câmera com a IA apontou. A da noite abre direto; as anteriores, pela lista; os números somam o período.
 */
@Component({
  selector: 'app-passagem',
  imports: [AvisoGuardado, CartoesNoCelular, Esqueleto, FORMULARIO, Estado, Nota],
  templateUrl: './passagem.html',
})
export class Passagem {
  protected readonly valorDe = valorDe;
  readonly aba = input<Aba>('noite');
  private readonly api = inject(Passagens);
  private readonly trabalho = inject(TrabalhoDaNoite);
  protected readonly semRede = this.trabalho.usarGuardado;
  protected readonly titulo = computed(() => TITULOS[this.aba()]);
  // Sem rede (no app): as passagens do pacote guardado (dos últimos 7 ou 30 dias).
  protected readonly lista = rxResource({
    params: () => ({ guardado: this.semRede() }),
    stream: ({ params }) => (params.guardado ? of(this.trabalho.pacote()?.passagens ?? []) : this.api.ultimas(30)),
  });
  protected readonly erro = computed(() => (this.lista.error() ? mensagemDeErro(this.lista.error()) : null));
  protected readonly escolhida = signal<number | null>(null);
  protected readonly idAberta = computed(() => (this.aba() === 'numeros' ? null : (this.escolhida() ?? (this.aba() === 'noite' ? (valorDe(this.lista)?.[0]?.id ?? null) : null))));
  protected readonly detalhe = rxResource({
    params: () => (this.idAberta() ? { id: this.idAberta()!, guardado: this.semRede() } : undefined),
    stream: ({ params }) => (params.guardado ? of(this.soResumo(params.id)) : this.api.buscar(params.id)),
  });
  protected readonly so = signal<'problemas' | 'todas'>('problemas');
  protected readonly leituras = computed(() => {
    const l = valorDe(this.detalhe)?.leituras ?? [];
    return this.so() === 'todas' ? l : l.filter((x) => !x.leitura || x.foraDePosicao || x.rachaduraSuspeita || x.idDormente === null);
  });
  protected readonly soma = computed(() => {
    const l = valorDe(this.lista) ?? [];
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

  /** Sem rede: só o resumo guardado; as leituras uma a uma abrem quando a rede voltar. */
  private soResumo(id: number): PassagemDetalheDto | null {
    const resumo = this.trabalho.pacote()?.passagens.find((p) => p.id === id);
    return resumo ? { resumo, veiculo: null, kmInicio: null, kmFim: null, limiteMm: 0, leituras: [] } : null;
  }

  protected abrir(p: PassagemDto): void {
    this.escolhida.set(p.id);
    this.so.set('problemas');
  }
}
