import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { dataHora, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { MetodoCaptura } from '../../../compartilhado/entidades/enums';
import { PecaDto } from '../../entidades/peca';
import { Catalogos } from '../../../catalogos/servicos/catalogos';
import { ClassificacaoDto, Classificacoes, MetodoClassificacao } from '../../servicos/classificacoes';
import { Pecas } from '../../servicos/pecas';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../../compartilhado/ui';

const METODOS: { valor: MetodoClassificacao; rotulo: string }[] = [
  { valor: 'Comparacao', rotulo: 'Comparação com a amostra' }, { valor: 'Visual', rotulo: 'Visual' },
  { valor: 'Catalogo', rotulo: 'Catálogo' }, { valor: 'Laboratorio', rotulo: 'Laboratório' }, { valor: 'Nir', rotulo: 'Pistola NIR' },
];
const NOME_METODO: Record<string, string> = { PlacaFornecedor: 'Placa do fornecedor', Comparacao: 'Comparação', Visual: 'Visual', Catalogo: 'Catálogo', Laboratorio: 'Laboratório', Nir: 'Pistola NIR' };

/**
 * Classificar (protótipo YBYRA, etapa 07; especialista): lê a peça, confirma a espécie da placa ou corrige.
 * A de antes nunca some; a classe vem da espécie. Em "Classificações feitas", as dos últimos 30 dias.
 */
@Component({
  selector: 'app-classificar',
  imports: [FormsModule, FORMULARIO, Estado, LeituraTag],
  templateUrl: './classificar.html',
})
export class Classificar {
  readonly aba = input<'peca' | 'feitas'>('peca');
  private readonly api = inject(Classificacoes);
  private readonly pecas = inject(Pecas);
  protected readonly especies = toSignal(inject(Catalogos).especies(), { initialValue: [] });
  protected readonly metodos = METODOS;
  protected readonly nomeMetodo = NOME_METODO;
  protected readonly metros = metros;
  protected readonly dataHora = dataHora;

  protected readonly feitas = rxResource({ params: () => (this.aba() === 'feitas' ? true : undefined), stream: () => this.api.recentes(30) });
  protected readonly erroFeitas = computed(() => (this.feitas.error() ? mensagemDeErro(this.feitas.error()) : null));

  protected readonly tag = signal('');
  protected readonly metodoLeitura = signal<MetodoCaptura>('ModuloUhf');
  protected readonly peca = signal<PecaDto | null>(null);
  protected readonly historico = signal<ClassificacaoDto[]>([]);
  protected readonly corrigir = signal(false);
  protected readonly idEspecie = signal<number | null>(null);
  protected readonly metodo = signal<MetodoClassificacao>('Comparacao');
  protected readonly observacao = signal('');
  /** a leitura da pistola NIR (só quando o método é a NIR) */
  protected readonly umidade = signal<number | null>(null);
  protected readonly confianca = signal<number | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected buscar(): void {
    if (!this.tag().trim()) return;
    this.erro.set(null);
    this.mensagem.set(null);
    this.corrigir.set(false);
    this.pecas.porTag(this.tag()).subscribe({
      next: (p) => {
        this.peca.set(p);
        this.idEspecie.set(p.idEspecie);
        this.api.daPeca(p.id).subscribe((h) => this.historico.set(h));
      },
      error: (e) => {
        this.peca.set(null);
        this.erro.set(e?.status === 404 ? 'Nenhuma peça com essa tag.' : mensagemDeErro(e));
      },
    });
  }

  /** Confirmar = a mesma espécie da placa; corrigir = outra espécie. */
  protected registrar(p: PecaDto, confirmar: boolean): void {
    const idEspecie = confirmar ? p.idEspecie : this.idEspecie();
    if (!idEspecie) return this.erro.set('Escolha a espécie.');
    const nir = this.metodo() === 'Nir';
    if (nir && this.umidade() === null) return this.erro.set('Informe a umidade que a pistola NIR mediu.');
    this.enviando.set(true);
    this.erro.set(null);
    this.api.classificar(p.id, idEspecie, this.metodo(), leitura(this.tag(), this.metodoLeitura()), this.observacao().trim() || null,
      nir ? { umidadePct: this.umidade(), confiancaPct: this.confianca() } : undefined).subscribe({
      next: (c) => {
        this.enviando.set(false);
        this.mensagem.set(confirmar ? `Espécie confirmada: ${c.especie}.` : `Espécie corrigida para ${c.especie}. A anterior continua no histórico.`);
        this.corrigir.set(false);
        this.observacao.set('');
        this.umidade.set(null);
        this.confianca.set(null);
        this.pecas.buscar(p.id).subscribe((n) => this.peca.set(n));
        this.api.daPeca(p.id).subscribe((h) => this.historico.set(h));
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }
}
