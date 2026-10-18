/** Abre um arquivo que veio da API (PDF do laudo, planilha) numa aba nova ou baixa com o nome dado. */
export const abrirArquivo = (arquivo: Blob, nome: string): void => {
  const url = URL.createObjectURL(arquivo);
  const link = document.createElement('a');
  link.href = url;
  if (arquivo.type === 'application/pdf') link.target = '_blank';
  else link.download = nome;
  link.rel = 'noopener';
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
};

/** Baixa o arquivo com o nome dado (o PDF do laudo, depois de visto na tela). */
export const baixarArquivo = (arquivo: Blob, nome: string): void => {
  const url = URL.createObjectURL(arquivo);
  const link = document.createElement('a');
  link.href = url;
  link.download = nome;
  link.rel = 'noopener';
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
};

/** Um identificador para o registro de campo não duplicar se a tela mandar duas vezes (id_cliente no back). */
export const novoIdCliente = (): string => crypto.randomUUID();