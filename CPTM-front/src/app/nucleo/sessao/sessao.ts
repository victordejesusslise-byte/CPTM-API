import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, map, of, shareReplay, tap } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { CARGOS, Cargo, TokenDto, UsuarioDto } from '../../pessoas/entidades/usuario';
import { Rede } from '../fila/rede';

const CHAVE = 'via-permanente.sessao';

/** O que fica no navegador: só o token e quando ele vence. Quem é a pessoa e o cargo não ficam (dá para editar pelo DevTools). */
interface SessaoGuardada {
  token: string;
  expiraEm: string;
}

/**
 * Quem entrou, lido de dentro do token. O token é assinado pela API: se alguém trocar o cargo nele, a assinatura deixa de bater e a
 * API recusa a próxima chamada (401), o que encerra a sessão.
 */
export const usuarioDoToken = (token: string | null): UsuarioDto | null => {
  try {
    const carga = token?.split('.')[1];
    if (!carga) return null;
    const base64 = carga.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(carga.length / 4) * 4, '=');
    const json = JSON.parse(new TextDecoder().decode(Uint8Array.from(atob(base64), (c) => c.charCodeAt(0))));
    const cargo = json.role as Cargo;
    if (!CARGOS.includes(cargo) || !json.sub) return null;
    return { id: Number(json.sub), nome: json.name ?? '', email: json.email ?? '', cargo, ativo: true };
  } catch {
    return null;
  }
};

/**
 * A sessão: o token JWT e quem entrou. O token fica no armazenamento do navegador para sobreviver ao recarregar; se o navegador
 * bloquear, a sessão vale só enquanto a página estiver aberta. Ao abrir o sistema, a guarda das rotas confere com a API quem é o
 * dono do token (GET /api/auth/eu) antes de mostrar qualquer tela (segurança, Victor, 2026-10-23). Quem decide o que cada cargo pode
 * fazer é sempre a API; o front só esconde o que não é do cargo.
 */
@Injectable({ providedIn: 'root' })
export class Sessao {
  private readonly http = inject(HttpClient);
  private readonly rede = inject(Rede);
  private readonly atual = signal<SessaoGuardada | null>(Sessao.ler());
  /** Quem a API confirmou (login ou /auth/eu). Fica só na memória. */
  private readonly confirmado = signal<UsuarioDto | null>(null);
  private conferindo: Observable<boolean> | null = null;

  readonly token = computed(() => this.atual()?.token ?? null);
  readonly usuario = computed<UsuarioDto | null>(() => (this.atual() ? (this.confirmado() ?? usuarioDoToken(this.token())) : null));
  /** Com o login vencido e sem rede, continua (app Android): o servidor pede a senha quando a rede voltar, antes de a fila enviar. */
  readonly logado = computed(() => {
    const s = this.atual();
    return !!s && !!usuarioDoToken(s.token) && (new Date(s.expiraEm).getTime() > Date.now() || !this.rede.online());
  });

  entrar(email: string, senha: string): Observable<TokenDto> {
    return this.http.post<TokenDto>(`${ambiente.api}/auth/login`, { email, senha }).pipe(
      tap((t) => {
        this.guardar({ token: t.token, expiraEm: t.expiraEm });
        this.confirmado.set(t.usuario);
      }),
    );
  }

  /**
   * Confere com a API quem é o dono do token (uma vez por abertura do sistema). Token adulterado, vencido ou de sessão encerrada:
   * a API responde 401 e a sessão acaba aqui.
   */
  confirmar(): Observable<boolean> {
    if (!this.logado()) return of(false);
    if (this.confirmado()) return of(true);
    this.conferindo ??= this.http.get<UsuarioDto>(`${ambiente.api}/auth/eu`).pipe(
      map((u) => {
        this.confirmado.set(u);
        return true;
      }),
      catchError((erro: unknown) => {
        // Sem rede: vale o token guardado; o servidor confere quando a rede voltar.
        if (erro instanceof HttpErrorResponse && erro.status === 0) return of(true);
        this.sair();
        return of(false);
      }),
      finalize(() => (this.conferindo = null)),
      shareReplay(1),
    );
    return this.conferindo;
  }

  /** Limpa a sessão deste aparelho. */
  sair(): void {
    this.confirmado.set(null);
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

  private guardar(valor: SessaoGuardada | null): void {
    this.atual.set(valor);
    try {
      if (valor) localStorage.setItem(CHAVE, JSON.stringify(valor));
      else localStorage.removeItem(CHAVE);
    } catch {
      /* navegador sem armazenamento: segue só em memória */
    }
  }

  private static ler(): SessaoGuardada | null {
    try {
      const texto = localStorage.getItem(CHAVE);
      const salvo = texto ? (JSON.parse(texto) as Partial<SessaoGuardada>) : null;
      return salvo?.token && salvo.expiraEm ? { token: salvo.token, expiraEm: salvo.expiraEm } : null;
    } catch {
      return null;
    }
  }
}
