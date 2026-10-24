import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Sessao } from '../../../nucleo/sessao/sessao';
import { Abertura } from '../../../nucleo/layout/abertura/abertura';
import { mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { Marca } from '../../../compartilhado/componentes/marca/marca';
import { FORMULARIO } from '../../../compartilhado/ui';

/** Entrar com o e-mail da pessoa e a senha dela (POST /api/auth/login). */
@Component({
  selector: 'app-login',
  imports: [FORMULARIO, Marca],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly sessao = inject(Sessao);
  private readonly abertura = inject(Abertura);
  private readonly router = inject(Router);
  private readonly rota = inject(ActivatedRoute);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    senha: ['', Validators.required],
  });
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected entrar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { email, senha } = this.form.getRawValue();
    this.enviando.set(true);
    this.erro.set(null);
    this.sessao.entrar(email.trim(), senha).subscribe({
      // No app, a tela com o logo fica alguns segundos ("Entrando…") antes de abrir o painel.
      next: async () => {
        await this.abertura.entrando();
        this.router.navigateByUrl(this.rota.snapshot.queryParamMap.get('voltar') ?? '/painel');
      },
      error: (e) => {
        this.erro.set(e?.status === 401 ? 'E-mail ou senha não conferem.' : mensagemDeErro(e));
        this.enviando.set(false);
      },
    });
  }
}