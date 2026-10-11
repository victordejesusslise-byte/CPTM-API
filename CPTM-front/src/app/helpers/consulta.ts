import { HttpParams } from '@angular/common/http';

/** Monta a query string sem os campos vazios: { idEmpresa: 3, papel: null } → ?idEmpresa=3 */
export const consulta = (valores: Record<string, string | number | boolean | null | undefined>): HttpParams =>
  Object.entries(valores).reduce(
    (p, [chave, valor]) => (valor === null || valor === undefined || valor === '' ? p : p.set(chave, String(valor))),
    new HttpParams(),
  );