import { Cargo } from '../../models/usuario';

/** Uma aba da tela: no computador aparece embaixo do item, no menu; no celular, como abas no alto. */
export interface SubMenu {
  rota: string;
  rotulo: string;
  curto?: string;
  soComputador?: boolean;
}

export interface ItemMenu {
  rota: string;
  subs?: SubMenu[];
  rotulo: string;
  grupo: 'Início' | 'Peça' | 'Campo' | 'Supervisão';
  cargos: Cargo[];
}

const todos: Cargo[] = ['Supervisor', 'Tecnico', 'Especialista', 'OperadorPatio', 'OperadorVia'];

/** O menu de cada cargo: cada um vê só o que pode fazer (Celular reduzido por cargo). As rotas e a API conferem de novo. */
export const MENU: ItemMenu[] = [
  { rota: '/painel', rotulo: 'Painel', grupo: 'Início', cargos: todos },
  {
    rota: '/pedidos', rotulo: 'Pedidos', grupo: 'Peça', cargos: ['Supervisor', 'OperadorPatio'],
    subs: [
      { rota: '/pedidos/novo', rotulo: 'Cadastrar pedido', curto: 'Cadastrar', soComputador: true },
      { rota: '/pedidos', rotulo: 'Acompanhar pedidos', curto: 'Acompanhar' },
    ],
  },
  { rota: '/chegadas', rotulo: 'Chegada da carga', grupo: 'Peça', cargos: ['Supervisor', 'OperadorPatio'] },
  { rota: '/cadastro', rotulo: 'Cadastro da peça', grupo: 'Peça', cargos: ['Supervisor', 'OperadorPatio'] },
  { rota: '/qualidade', rotulo: 'Verificação de qualidade', grupo: 'Peça', cargos: ['Supervisor', 'Especialista'] },
  { rota: '/armazem', rotulo: 'Armazém', grupo: 'Peça', cargos: ['Supervisor', 'OperadorPatio', 'Especialista'] },
  { rota: '/pecas', rotulo: 'Consultar peça', grupo: 'Peça', cargos: todos },
  { rota: '/alertas', rotulo: 'Alertas', grupo: 'Campo', cargos: ['Supervisor', 'Tecnico', 'OperadorVia'] },
  { rota: '/inspecoes', rotulo: 'Inspeção', grupo: 'Campo', cargos: ['Tecnico'] },
  { rota: '/trocas', rotulo: 'Trocas', grupo: 'Campo', cargos: todos },
  { rota: '/indicadores', rotulo: 'Indicadores', grupo: 'Supervisão', cargos: todos },
  { rota: '/relatorio-ambiental', rotulo: 'Relatório ambiental', grupo: 'Supervisão', cargos: ['Supervisor', 'Especialista'] },
  { rota: '/cadastros', rotulo: 'Cadastros', grupo: 'Supervisão', cargos: ['Supervisor'] },
  { rota: '/usuarios', rotulo: 'Usuários', grupo: 'Supervisão', cargos: ['Supervisor'] },
];

export const menuDoCargo = (cargo: Cargo | null | undefined): ItemMenu[] => (cargo ? MENU.filter((i) => i.cargos.includes(cargo)) : []);