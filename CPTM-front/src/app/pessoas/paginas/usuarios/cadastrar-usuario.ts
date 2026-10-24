import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { mensagemDeErro, nomeDoCargo } from '../../../compartilhado/utilitarios/formato';
import { CARGOS, Cargo } from '../../entidades/usuario';
import { Usuarios } from '../../servicos/usuarios';
import { FORMULARIO } from '../../../compartilhado/ui';


/** Usuários → Cadastrar usuário (supervisor): nome, e-mail, cargo e a senha inicial da pessoa. */
@Component({
  selector: 'app-cadastrar-usuario',
  imports: [RouterLink, FORMULARIO],
  template: `
    <p class="sobre">Supervisão</p>
    <h1>Cadastrar usuário</h1>
    <form class="cartao pilha" [formGroup]="form" (ngSubmit)="salvar()" novalidate>
      <header><h2>Nova pessoa</h2><span class="nota">ela entra com este e-mail e esta senha</span></header>
      <div class="campos">
        <mat-form-field appearance="outline"><mat-label>Nome</mat-label><input matInput formControlName="nome" autocomplete="off" /><mat-error>Informe o nome.</mat-error></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>E-mail</mat-label><input matInput type="email" formControlName="email" autocomplete="off" /><mat-error>Confira o e-mail.</mat-error></mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Cargo</mat-label>
          <mat-select formControlName="cargo">
            @for (c of cargos; track c) { <mat-option [value]="c">{{ nomeDoCargo(c) }}</mat-option> }
          </mat-select>
          <mat-error>Escolha o cargo.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Senha inicial</mat-label><input matInput type="password" formControlName="senha" autocomplete="new-password" />
          <mat-hint>Pelo menos 8 caracteres. Passe para a pessoa em particular.</mat-hint>
          <mat-error>Pelo menos 8 caracteres.</mat-error>
        </mat-form-field>
      </div>
      @if (feito()) { <p class="aviso aviso--ok" role="status">{{ feito() }}</p> }
      @if (erro()) { <p class="aviso aviso--erro" role="alert">{{ erro() }}</p> }
      <div class="acoes"><button mat-flat-button type="submit" [disabled]="enviando()" [appCarregando]="enviando()">Cadastrar usuário</button></div>
    </form>
    <p class="nota">Cadastrada, a pessoa aparece em <a routerLink="/usuarios">Usuários cadastrados</a>.</p>
  `,
})
export class CadastrarUsuario {
  private readonly usuarios = inject(Usuarios);
  protected readonly cargos = CARGOS;
  protected readonly nomeDoCargo = nomeDoCargo;
  protected readonly form = inject(NonNullableFormBuilder).group({
    nome: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    cargo: ['' as Cargo | '', Validators.required],
    senha: ['', [Validators.required, Validators.minLength(8)]],
  });
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);
  protected readonly feito = signal<string | null>(null);

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.enviando.set(true);
    this.erro.set(null);
    this.feito.set(null);
    this.usuarios.criar({ nome: v.nome.trim(), email: v.email.trim(), senha: v.senha, cargo: v.cargo as Cargo }).subscribe({
      next: (u) => {
        this.feito.set(`${u.nome} cadastrado como ${nomeDoCargo(u.cargo)}. Já pode entrar com ${u.email}.`);
        this.form.reset();
        this.enviando.set(false);
      },
      error: (e) => {
        this.erro.set(mensagemDeErro(e));
        this.enviando.set(false);
      },
    });
  }
}