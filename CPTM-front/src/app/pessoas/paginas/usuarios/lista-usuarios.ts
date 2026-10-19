import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { dataHora, mensagemDeErro, nomeDoCargo } from '../../../compartilhado/utilitarios/formato';
import { UsuarioDto } from '../../entidades/usuario';
import { Usuarios } from '../../servicos/usuarios';
import { Sessao } from '../../../nucleo/sessao/sessao';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { FORMULARIO } from '../../../compartilhado/ui';

/**
 * Usuários → Usuários cadastrados (supervisor). Desativar derruba na hora as sessões abertas da conta (segurança, 2026-10-18);
 * a conta que está esperando por senhas erradas seguidas aparece em vermelho, com a hora. Ninguém desativa a própria conta.
 */
@Component({
  selector: 'app-lista-usuarios',
  imports: [RouterLink, Estado, FORMULARIO],
  template: `
    <p class="sobre">Supervisão</p>
    <h1>Usuários cadastrados</h1>
    <div class="acoes"><a mat-flat-button routerLink="/usuarios/novo">Cadastrar usuário</a></div>
    @if (mensagem()) { <p class="aviso aviso--ok" role="status">{{ mensagem() }}</p> }
    @if (erroAcao()) { <p class="aviso aviso--erro" role="alert">{{ erroAcao() }}</p> }
    <app-estado esqueleto="tabela" [carregando]="usuarios.isLoading()" [erro]="erro()" [vazio]="usuarios.hasValue() && !usuarios.value().length" />
    @if (usuarios.value()?.length) {
      <div class="tabela"><table class="simples">
        <thead><tr><th>Nome</th><th>E-mail</th><th>Cargo</th><th>Situação</th><th></th></tr></thead>
        <tbody>@for (u of usuarios.value(); track u.id) {
          <tr>
            <td>{{ u.nome }}</td><td class="mono">{{ u.email }}</td><td>{{ nomeDoCargo(u.cargo) }}</td>
            <td [class.vermelho]="!u.ativo || u.esperarAte">{{ situacao(u) }}</td>
            <td>
              @if (u.id !== idProprio()) {
                <button mat-button type="button" (click)="definirAtivo(u, !u.ativo)" [disabled]="enviando()" [appCarregando]="enviando()">{{ u.ativo ? 'Desativar' : 'Reativar' }}</button>
              }
            </td>
          </tr>
        }</tbody>
      </table></div>
    }
  `,
})
export class ListaUsuarios {
  private readonly servico = inject(Usuarios);
  protected readonly usuarios = rxResource({ stream: () => this.servico.listar() });
  protected readonly erro = computed(() => (this.usuarios.error() ? mensagemDeErro(this.usuarios.error()) : null));
  private readonly sessao = inject(Sessao);
  protected readonly idProprio = computed(() => this.sessao.usuario()?.id ?? null);
  protected readonly nomeDoCargo = nomeDoCargo;
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erroAcao = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected situacao(u: UsuarioDto): string {
    if (!u.ativo) return 'Inativo';
    return u.esperarAte ? `Esperando até ${dataHora(u.esperarAte)} (senhas erradas)` : 'Ativo';
  }

  protected definirAtivo(u: UsuarioDto, ativo: boolean): void {
    this.enviando.set(true);
    this.mensagem.set(null);
    this.erroAcao.set(null);
    this.servico.definirAtivo(u.id, ativo).subscribe({
      next: () => {
        this.enviando.set(false);
        this.mensagem.set(ativo ? `${u.nome} pode entrar de novo.` : `${u.nome} foi desativado: as sessões abertas caíram na hora.`);
        this.usuarios.reload();
      },
      error: (e) => {
        this.enviando.set(false);
        this.erroAcao.set(mensagemDeErro(e));
      },
    });
  }
}
