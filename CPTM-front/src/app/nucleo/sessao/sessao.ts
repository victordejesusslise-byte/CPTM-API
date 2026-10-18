import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, of, tap } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { Cargo, TokenDto, UsuarioDto } from '../../pessoas/entidades/usuario';

const CHAVE = 'via-permanente.sessao';

/**
 * A sessão: o token JWT e quem entrou. Fica no armazenamento do navegador para sobreviver ao recarregar;
 * se o navegador bloquear, a sessão vale só enquanto a página estiver aberta.
 */
@Injectable({ providedIn: 'root' })
export class Sessao {
  private readonly http = inject(HttpClient);
  private readonly atual = signal<TokenDto | null>(Sessao.ler());

  readonly usuario = computed<UsuarioDto | null>(() => this.atual()?.usuario ?? null);
  readonly token = computed(() => this.atual()?.token ?? null);
  readonly logado = computed(() => {
    const s = this.atual();
    return !!s && new Date(s.expiraEm).getTime() > Date.now();
  });

  entrar(email: string, senha: string): Observable<TokenDto> {
    return this.http.post<TokenDto>(`${ambiente.api}/auth/login`, { email, senha }).pipe(tap((t) => this.guardar(t)));
  }

  /** Limpa a sessão deste aparelho. */
  sair(): void {
    this.guardar(null);
  }

  /** Sair de verdade: a API encerra a sessão em todos os aparelhos; mesmo sem rede, este aparelho sai. */
  encerrar(): Observable<void> {
    const pedido = this.token() ? this.http.post<void>(`${ambiente.api}/auth/sair`, {}) : of(undefined);
    return pedido.pipe(
      catchError(() => of(undefined)),
      finalize(() => this.sair()),
    );
  }

  temCargo(...cargos: Cargo[]): boolean {
    const cargo = this.usuario()?.cargo;
    return !!cargo && cargos.includes(cargo);
  }

  private guardar(valor: TokenDto | null): void {
    this.atual.set(valor);
    try {
      if (valor) localStorage.setItem(CHAVE, JSON.stringify(valor));
      else localStorage.removeItem(CHAVE);
    } catch {
      /* navegador sem armazenamento: segue só em memória */
    }
  }

  private static ler(): TokenDto | null {
    try {
      const texto = localStorage.getItem(CHAVE);
      return texto ? (JSON.parse(texto) as TokenDto) : null;
    } catch {
      return null;
    }
  }
}