import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, InjectionToken, effect, inject, signal, untracked } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { firstValueFrom } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { PecaDto } from '../../pecas/entidades/peca';
import { NO_APP } from '../ao-vivo/trabalho-da-noite';
import { Rede } from '../fila/rede';
import { Sessao } from '../sessao/sessao';

/** Uma peça na cópia das linhas (GET /api/campo/linha): só o essencial. */
export interface PecaDaLinha {
  id: number;
  codigo: string;
  tag: string | null;
  km: number | null;
  posicao: string | null;
  idTrecho: number | null;
  situacao: string;
  nota: string | null;
  ultimaInspecao: string | null;
  ultimaInspecaoEm: string | null;
}

interface PaginaDaLinha {
  pecas: PecaDaLinha[];
  proximo: number | null;
  geradoEm: string;
}

/** Onde a cópia mora: no celular, o banco SQLite (aguenta centenas de milhares de peças e acha a tag na hora); no navegador e nos testes, a memória. */
export interface ArmazemDaLinha {
  gravar(pecas: PecaDaLinha[]): Promise<void>;
  apagar(ids: number[]): Promise<void>;
  porTag(tag: string): Promise<PecaDaLinha | null>;
  porCodigo(codigo: string): Promise<PecaDaLinha | null>;
  porTrecho(idTrecho: number): Promise<PecaDaLinha[]>;
  contar(): Promise<number>;
  lerSincronia(): Promise<string | null>;
  gravarSincronia(geradoEm: string): Promise<void>;
}

class NaMemoria implements ArmazemDaLinha {
  private readonly pecas = new Map<number, PecaDaLinha>();
  private sincronia: string | null = null;
  async gravar(pecas: PecaDaLinha[]): Promise<void> {
    for (const p of pecas) this.pecas.set(p.id, p);
  }
  async apagar(ids: number[]): Promise<void> {
    for (const id of ids) this.pecas.delete(id);
  }
  async porTag(tag: string): Promise<PecaDaLinha | null> {
    return [...this.pecas.values()].find((p) => p.tag === tag) ?? null;
  }
  async porCodigo(codigo: string): Promise<PecaDaLinha | null> {
    return [...this.pecas.values()].find((p) => p.codigo === codigo) ?? null;
  }
  async porTrecho(idTrecho: number): Promise<PecaDaLinha[]> {
    return [...this.pecas.values()].filter((p) => p.idTrecho === idTrecho).sort((a, b) => (a.km ?? 0) - (b.km ?? 0));
  }
  async contar(): Promise<number> {
    return this.pecas.size;
  }
  async lerSincronia(): Promise<string | null> {
    return this.sincronia;
  }
  async gravarSincronia(geradoEm: string): Promise<void> {
    this.sincronia = geradoEm;
  }
}

const COLUNAS = ['id', 'codigo', 'tag', 'km', 'posicao', 'id_trecho', 'situacao', 'nota', 'ultima_inspecao', 'ultima_inspecao_em'];
const daLinha = (r: Record<string, unknown>): PecaDaLinha => ({
  id: Number(r['id']), codigo: String(r['codigo']), tag: (r['tag'] as string) ?? null, km: (r['km'] as number) ?? null, posicao: (r['posicao'] as string) ?? null,
  idTrecho: (r['id_trecho'] as number) ?? null, situacao: String(r['situacao']), nota: (r['nota'] as string) ?? null,
  ultimaInspecao: (r['ultima_inspecao'] as string) ?? null, ultimaInspecaoEm: (r['ultima_inspecao_em'] as string) ?? null,
});

class NoSqlite implements ArmazemDaLinha {
  private conexao?: Promise<import('@capacitor-community/sqlite').SQLiteDBConnection>;

  private abrir(): Promise<import('@capacitor-community/sqlite').SQLiteDBConnection> {
    this.conexao ??= (async () => {
      const { CapacitorSQLite, SQLiteConnection } = await import('@capacitor-community/sqlite');
      const sqlite = new SQLiteConnection(CapacitorSQLite);
      const nome = 'ybyra_linha';
      const consistente = (await sqlite.checkConnectionsConsistency()).result;
      const existe = (await sqlite.isConnection(nome, false)).result;
      const banco = consistente && existe ? await sqlite.retrieveConnection(nome, false) : await sqlite.createConnection(nome, false, 'no-encryption', 1, false);
      await banco.open();
      await banco.execute(`CREATE TABLE IF NOT EXISTS peca (id INTEGER PRIMARY KEY, codigo TEXT NOT NULL, tag TEXT, km REAL, posicao TEXT, id_trecho INTEGER,
          situacao TEXT NOT NULL, nota TEXT, ultima_inspecao TEXT, ultima_inspecao_em TEXT);
        CREATE INDEX IF NOT EXISTS ix_peca_tag ON peca (tag);
        CREATE INDEX IF NOT EXISTS ix_peca_codigo ON peca (codigo);
        CREATE INDEX IF NOT EXISTS ix_peca_trecho ON peca (id_trecho, km);
        CREATE TABLE IF NOT EXISTS sincronia (chave TEXT PRIMARY KEY, valor TEXT);`);
      return banco;
    })();
    return this.conexao;
  }

  async gravar(pecas: PecaDaLinha[]): Promise<void> {
    if (!pecas.length) return;
    const banco = await this.abrir();
    const sql = `INSERT OR REPLACE INTO peca (${COLUNAS.join(', ')}) VALUES (${COLUNAS.map(() => '?').join(', ')})`;
    await banco.executeSet(
      pecas.map((p) => ({ statement: sql, values: [p.id, p.codigo, p.tag, p.km, p.posicao, p.idTrecho, p.situacao, p.nota, p.ultimaInspecao, p.ultimaInspecaoEm] })),
      true,
    );
  }
  async apagar(ids: number[]): Promise<void> {
    if (!ids.length) return;
    await (await this.abrir()).run(`DELETE FROM peca WHERE id IN (${ids.map(() => '?').join(', ')})`, ids);
  }
  private async uma(onde: string, valor: string): Promise<PecaDaLinha | null> {
    const r = await (await this.abrir()).query(`SELECT * FROM peca WHERE ${onde} = ? LIMIT 1`, [valor]);
    return r.values?.length ? daLinha(r.values[0]) : null;
  }
  porTag(tag: string): Promise<PecaDaLinha | null> {
    return this.uma('tag', tag);
  }
  porCodigo(codigo: string): Promise<PecaDaLinha | null> {
    return this.uma('codigo', codigo);
  }
  async porTrecho(idTrecho: number): Promise<PecaDaLinha[]> {
    const r = await (await this.abrir()).query('SELECT * FROM peca WHERE id_trecho = ? ORDER BY km', [idTrecho]);
    return (r.values ?? []).map(daLinha);
  }
  async contar(): Promise<number> {
    const r = await (await this.abrir()).query('SELECT COUNT(*) AS n FROM peca');
    return Number(r.values?.[0]?.['n'] ?? 0);
  }
  async lerSincronia(): Promise<string | null> {
    const r = await (await this.abrir()).query(`SELECT valor FROM sincronia WHERE chave = 'linha'`);
    return (r.values?.[0]?.['valor'] as string) ?? null;
  }
  async gravarSincronia(geradoEm: string): Promise<void> {
    await (await this.abrir()).run(`INSERT OR REPLACE INTO sincronia (chave, valor) VALUES ('linha', ?)`, [geradoEm]);
  }
}

export const ARMAZEM_DA_LINHA = new InjectionToken<ArmazemDaLinha>('Cópia das linhas', {
  providedIn: 'root',
  factory: () => (Capacitor.isNativePlatform() ? new NoSqlite() : new NaMemoria()),
});

const PAGINA = 2000;
const DE_TEMPOS_EM_TEMPOS_MS = 30 * 60_000;

/** A peça da cópia no formato das telas (o que não está na cópia fica vazio). */
export const comoPecaDto = (p: PecaDaLinha): PecaDto =>
  ({ id: p.id, codigo: p.codigo, tag: p.tag, kmProjeto: p.km, posicao: p.posicao, idTrecho: p.idTrecho, situacao: p.situacao, nota: p.nota }) as PecaDto;

/**
 * A cópia de todas as linhas no celular (app, Entrega 2, 2026-10-25): com qualquer rede, baixa as peças das linhas em páginas
 * (a primeira vez, tudo; depois, só o que mudou desde a última) e apaga as que saíram da via. Sem rede, as telas acham qualquer peça
 * pela tag ou pelo código. Sincroniza ao entrar, quando a rede volta, a cada 30 min e pelo botão "Atualizar as linhas agora".
 */
@Injectable({ providedIn: 'root' })
export class CopiaDaLinha {
  private readonly http = inject(HttpClient);
  private readonly sessao = inject(Sessao);
  private readonly rede = inject(Rede);
  private readonly armazem = inject(ARMAZEM_DA_LINHA);

  readonly sincronizando = signal(false);
  /** Quantas peças já vieram nesta sincronização (para "Baixando as linhas · N peças"). */
  readonly baixadas = signal(0);
  readonly total = signal(0);
  readonly ultimaSincronia = signal<string | null>(null);

  constructor() {
    void this.armazem.contar().then((n) => this.total.set(n)).catch(() => undefined);
    void this.armazem.lerSincronia().then((s) => this.ultimaSincronia.set(s)).catch(() => undefined);
    if (!inject(NO_APP)) return;
    effect(() => {
      if (this.sessao.usuario() && this.rede.online()) untracked(() => void this.sincronizar());
    });
    const relogio = setInterval(() => void this.sincronizar(), DE_TEMPOS_EM_TEMPOS_MS);
    inject(DestroyRef).onDestroy(() => clearInterval(relogio));
  }

  /** Baixa o que falta. Sem rede, sem login ou já baixando, não faz nada. */
  async sincronizar(): Promise<void> {
    if (this.sincronizando() || !this.rede.online() || !this.sessao.usuario()) return;
    this.sincronizando.set(true);
    this.baixadas.set(0);
    try {
      const desde = await this.armazem.lerSincronia();
      let depoisDe = 0;
      let geradoEm: string | null = null;
      for (;;) {
        const params: Record<string, string> = { depoisDe: String(depoisDe), limite: String(PAGINA) };
        if (desde) params['desde'] = desde;
        const pagina = await firstValueFrom(this.http.get<PaginaDaLinha>(`${ambiente.api}/campo/linha`, { params }));
        geradoEm ??= pagina.geradoEm;
        await this.armazem.apagar(pagina.pecas.filter((p) => p.situacao === 'Retirada').map((p) => p.id));
        await this.armazem.gravar(pagina.pecas.filter((p) => p.situacao !== 'Retirada'));
        this.baixadas.update((n) => n + pagina.pecas.length);
        if (pagina.proximo === null) break;
        depoisDe = pagina.proximo;
      }
      await this.armazem.gravarSincronia(geradoEm!);
      this.ultimaSincronia.set(geradoEm);
      this.total.set(await this.armazem.contar());
    } catch {
      /* sem rede no meio: na próxima, continua de onde a última cópia completa parou */
    } finally {
      this.sincronizando.set(false);
    }
  }

  async pecaPorTag(tag: string): Promise<PecaDto | null> {
    const p = await this.armazem.porTag(tag.trim());
    return p ? comoPecaDto(p) : null;
  }

  /** As peças de um trecho, pelo km (o mapa do trecho sem rede). */
  pecasDoTrecho(idTrecho: number): Promise<PecaDaLinha[]> {
    return this.armazem.porTrecho(idTrecho);
  }

  async pecaPorCodigo(codigo: string): Promise<PecaDto | null> {
    const p = await this.armazem.porCodigo(codigo.trim().toUpperCase());
    return p ? comoPecaDto(p) : null;
  }
}
