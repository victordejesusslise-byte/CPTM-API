import { Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { mensagemDeErro, nomeDoCargo } from '../../../compartilhado/utilitarios/formato';
import { UsuarioDto } from '../../entidades/usuario';
import { Usuarios } from '../../servicos/usuarios';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { Coluna, Tabela } from '../../../compartilhado/componentes/tabela/tabela';
import { FORMULARIO } from '../../../compartilhado/ui';

/** Usuários → Usuários cadastrados (supervisor). */
@Component({
  selector: 'app-lista-usuarios',
  imports: [RouterLink, Estado, Tabela, FORMULARIO],
  template: `
    <p class="sobre">Supervisão</p>
    <h1>Usuários cadastrados</h1>
    <div class="acoes"><a mat-flat-button routerLink="/usuarios/novo">Cadastrar usuário</a></div>
    <app-estado esqueleto="tabela" [carregando]="usuarios.isLoading()" [erro]="erro()" [vazio]="usuarios.hasValue() && !usuarios.value().length" />
    @if (usuarios.value()?.length) {
      <app-tabela [colunas]="colunas" [linhas]="usuarios.value() ?? []" />
    }
  `,
})
export class ListaUsuarios {
  private readonly servico = inject(Usuarios);
  protected readonly usuarios = rxResource({ stream: () => this.servico.listar() });
  protected readonly erro = computed(() => (this.usuarios.error() ? mensagemDeErro(this.usuarios.error()) : null));
  protected readonly colunas: Coluna<UsuarioDto>[] = [
    { titulo: 'Nome', valor: (u) => u.nome },
    { titulo: 'E-mail', valor: (u) => u.email },
    { titulo: 'Cargo', valor: (u) => nomeDoCargo(u.cargo) },
    { titulo: 'Situação', valor: (u) => (u.ativo ? 'Ativo' : 'Inativo'), vermelho: (u) => !u.ativo },
  ];
}