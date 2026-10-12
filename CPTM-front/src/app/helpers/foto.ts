/**
 * A foto vai como o nome do arquivo (FotoArquivo), como o back guarda hoje. O envio do arquivo para o servidor
 * espera a decisão "Onde ficam as fotos"; até lá a tela registra o nome em fotos/campo/.
 */
export const nomeDaFoto = (arquivo: File | null | undefined, prefixo: string): string =>
  arquivo ? `fotos/campo/${prefixo}-${Date.now()}-${arquivo.name.replace(/[^\w.-]+/g, '_')}` : '';