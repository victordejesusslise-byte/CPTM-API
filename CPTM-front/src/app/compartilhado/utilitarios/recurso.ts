import { Resource } from '@angular/core';

/**
 * O valor de uma busca (resource), ou undefined enquanto ela não tem valor: carregando ou com erro.
 * No Angular 22, ler value() de uma busca com erro lança (ResourceValueError) e a tela trava em "Carregando…" em vez de mostrar o erro
 * (Victor, 2026-10-31: "Cargas e boletins" do especialista). Toda tela lê as buscas por aqui.
 */
export const valorDe = <T>(busca: Resource<T>): T | undefined => (busca.hasValue() ? busca.value() : undefined);
