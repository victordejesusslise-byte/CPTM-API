import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { Nota } from '../../../compartilhado/componentes/nota/nota';
import { finalize, merge, switchMap } from 'rxjs';
import { FormBuilder, Validators } from '@angular/forms';
import { novoIdCliente } from '../../../compartilhado/utilitarios/arquivo';
import { mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { AtaqueBiologicoVerificacao, MetodoCaptura, RachaduraVerificacao, ResultadoVerificacao } from '../../../compartilhado/entidades/enums';
import { PecaDto } from '../../../pecas/entidades/peca';
import { Pecas } from '../../../pecas/servicos/pecas';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../../compartilhado/ui';
import { LegivelPipe } from '../../../compartilhado/utilitarios/legivel';

/** O defeito que reprova a peça, já escrito para o motivo; sem ataque biológico nem rachadura, nulo. */
export const defeitoDaVerificacao = (ataque: AtaqueBiologicoVerificacao, rachadura: RachaduraVerificacao): string | null => {
  const partes = [ataque !== 'Nenhum' ? 'ataque biológico ' + ataque.toLowerCase() : null, rachadura !== 'Nenhuma' ? 'rachadura ' + rachadura.toLowerCase() : null]
    .filter((p): p is string => !!p);
  if (!partes.length) return null;
  const texto = partes.join('; ');
  return texto[0].toUpperCase() + texto.slice(1);
};

/**
 * A verificação de qualidade (especialista): lê a peça, marca o que viu e aprova ou reprova. Com ataque biológico ou rachadura, de
 * qualquer grau, o resultado vira Reprovada sozinho, o motivo já vem escrito e Aprovada fica travada (Victor, 2026-10-31: evitar erro
 * humano); o servidor confere de novo.
 */
@Component({
  selector: 'app-qualidade',
  imports: [LegivelPipe, Esqueleto, FORMULARIO, LeituraTag, Nota],
  templateUrl: './qualidade.html',
})
export class Qualidade {
  /** Buscando a peça pela tag: o cartão dela mostra o skeleton. */
  protected readonly buscando = signal(false);
  private readonly pecas = inject(Pecas);
  protected readonly metros = metros;
  protected readonly tag = signal('');
  protected readonly metodo = signal<MetodoCaptura>('ModuloUhf');
  protected readonly peca = signal<PecaDto | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);
  /** a foto da entrada: fica guardada com o ID da peça, para a IA comparar depois com a câmera da via */
  protected readonly foto = signal<File | null>(null);

  protected escolherFoto(e: Event): void {
    this.foto.set((e.target as HTMLInputElement).files?.[0] ?? null);
  }

  protected readonly form = inject(FormBuilder).nonNullable.group({
    ataqueBiologico: ['Nenhum' as AtaqueBiologicoVerificacao],
    rachadura: ['Nenhuma' as RachaduraVerificacao],
    placaAntirrachante: [true],
    placaConfere: [true],
    resultado: ['Aprovada' as ResultadoVerificacao, Validators.required],
    motivo: [''],
  });
  /** Tem ataque biológico ou rachadura: Aprovada fica travada. */
  protected readonly temDefeito = signal(false);

  constructor() {
    let motivoAutomatico = '';
    const { ataqueBiologico, rachadura, resultado, motivo } = this.form.controls;
    merge(ataqueBiologico.valueChanges, rachadura.valueChanges)
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        const defeito = defeitoDaVerificacao(ataqueBiologico.value, rachadura.value);
        // o motivo que o especialista escreveu fica; só o escrito aqui é trocado
        const motivoEraAutomatico = !motivo.value.trim() || motivo.value === motivoAutomatico;
        if (defeito) {
          resultado.setValue('Reprovada');
          if (motivoEraAutomatico) motivo.setValue(defeito);
          motivoAutomatico = defeito;
        } else if (motivoAutomatico) {
          resultado.setValue('Aprovada');
          if (motivoEraAutomatico) motivo.setValue('');
          motivoAutomatico = '';
        }
        this.temDefeito.set(!!defeito);
      });
  }

  protected buscar(): void {
    if (!this.tag().trim()) return;
    this.erro.set(null);
    this.mensagem.set(null);
    this.buscando.set(true);
    this.pecas.porTag(this.tag()).pipe(finalize(() => this.buscando.set(false))).subscribe({
      next: (p) => this.peca.set(p),
      error: (e) => {
        this.peca.set(null);
        this.erro.set(e?.status === 404 ? 'Nenhuma peça com essa tag.' : mensagemDeErro(e));
      },
    });
  }

  protected registrar(): void {
    const p = this.peca();
    if (!p) return;
    const v = this.form.getRawValue();
    if (v.resultado === 'Reprovada' && !v.motivo.trim()) {
      this.erro.set('Diga o motivo da reprovação.');
      return;
    }
    this.enviando.set(true);
    this.erro.set(null);
    const foto = this.foto();
    if (!foto) {
      this.erro.set('Tire a foto da peça: ela fica guardada com o ID dela.');
      this.enviando.set(false);
      return;
    }
    this.pecas
      .enviarFoto(p.id, foto)
      .pipe(switchMap((r) => this.pecas.verificar(p.id, { ...v, motivo: v.motivo || null, fotoArquivo: r.arquivo, leitura: leitura(this.tag(), this.metodo()), idCliente: novoIdCliente() })))
      .subscribe({
      next: (r) => {
        this.mensagem.set(`Peça ${r.codigo}: ${v.resultado.toLowerCase()}. Situação agora: ${r.situacao}.`);
        this.peca.set(null);
        this.tag.set('');
        this.form.reset();
        this.foto.set(null);
        this.enviando.set(false);
      },
      error: (e) => {
        this.erro.set(mensagemDeErro(e));
        this.enviando.set(false);
      },
    });
  }
}