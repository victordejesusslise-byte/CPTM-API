import { EspecieDto, TamanhoDto } from '../../../catalogos/entidades/catalogo';
import { feixeDaPlaca } from './placa-no-feixe';

const especies: EspecieDto[] = [
  { id: 1, nomeCientifico: 'Ocotea neesiana', familia: 'Lauraceae', nomePopular: 'Canela' },
  { id: 3, nomeCientifico: 'Hymenaea courbaril', familia: 'Fabaceae', nomePopular: 'Jatobá' },
];
const tamanhos: TamanhoDto[] = [
  { id: 1, comprimentoM: 2.8 },
  { id: 2, comprimentoM: 3 },
];

describe('feixeDaPlaca', () => {
  it('preenche o feixe com a placa real lida pela foto', () => {
    const feixe = feixeDaPlaca({ EspecieClasse: 'OCOTEA NEESIANA, LAURACEAE / 1a', Comprimento: '2,80 M', Tratamento: 'CCA' }, especies, tamanhos);
    expect(feixe).toEqual({ idEspecie: 1, idTamanho: 1, classe: 1, tratamento: 'CCA' });
  });

  it('acha a espécie pelo nome popular e a 2ª classe', () => {
    expect(feixeDaPlaca({ EspecieClasse: 'Jatobá / 2ª', Comprimento: '3,00 m' }, especies, tamanhos)).toEqual({ idEspecie: 3, idTamanho: 2, classe: 2, tratamento: null });
  });

  it('deixa nulo o que a placa não disse ou não deu para reconhecer', () => {
    expect(feixeDaPlaca({ EspecieClasse: 'ER ECIASSE', Comprimento: '4,50 M' }, especies, tamanhos)).toEqual({ idEspecie: null, idTamanho: null, classe: null, tratamento: null });
  });
});
