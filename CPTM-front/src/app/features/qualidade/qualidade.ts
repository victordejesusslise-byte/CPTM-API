import { Component, inject, signal } from '@angular/core';
import { switchMap } from 'rxjs';
import { FormBuilder, Validators } from '@angular/forms';
import { novoIdCliente } from '../../helpers/arquivo';
import { mensagemDeErro, metros } from '../../helpers/formato';
import { AtaqueBiologicoVerificacao, MetodoCaptura, RachaduraVerificacao, ResultadoVerificacao } from '../../models/enums';
import { PecaDto } from '../../models/peca';
import { Pecas } from '../../services/pecas';
import { LeituraTag, leitura } from '../../shared/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../shared/ui';

/** A verificação de qualidade (especialista): lê a peça, marca o que viu e aprova ou reprova. */
@Component({
  selector: 'app-qualidade',
  imports: [FORMULARIO, LeituraTag],
  templateUrl: './qualidade.html',
})
export class Qualidade {
  private readonly pecas = inject(Pecas);
  protected readonly metros = metros;
  protected readonly tag = signal('');
  protected readonly metodo = signal<MetodoCaptura>('Pistola');
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

  protected buscar(): void {
    if (!this.tag().trim()) return;
    this.erro.set(null);
    this.mensagem.set(null);
    this.pecas.porTag(this.tag()).subscribe({
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