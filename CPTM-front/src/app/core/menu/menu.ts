import { Cargo } from '../../models/usuario';

/** Uma aba da tela: no computador aparece embaixo do item, no menu; no celular, como abas no alto. */
export interface SubMenu {
  rota: string;
  rotulo: string;
  curto?: string;
  soComputador?: boolean;
  /** só estes cargos veem a aba */
  cargos?: Cargo[];
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
  {
    rota: '/painel', rotulo: 'Painel', grupo: 'Início', cargos: todos,
    subs: [{ rota: '/painel', rotulo: 'O que fazer hoje', curto: 'Hoje' }, { rota: '/painel/numeros', rotulo: 'Acompanhamento', curto: 'Números' }],
  },
  {
    rota: '/pedidos', rotulo: 'Pedidos', grupo: 'Peça', cargos: ['Supervisor', 'OperadorPatio'],
    subs: [
      { rota: '/pedidos/novo', rotulo: 'Cadastrar pedido', curto: 'Cadastrar', soComputador: true },
      { rota: '/pedidos', rotulo: 'Acompanhar pedidos', curto: 'Acompanhar' },
    ],
  },
  {
    rota: '/chegadas', rotulo: 'Chegada da carga', grupo: 'Peça', cargos: ['Supervisor', 'OperadorPatio'],
    subs: [{ rota: '/chegadas', rotulo: 'Registrar chegada', curto: 'Registrar' }, { rota: '/chegadas/entregas', rotulo: 'Entregas recebidas', curto: 'Entregas' }],
  },
  {
    rota: '/cadastro', rotulo: 'Cadastro da peça', grupo: 'Peça', cargos: ['Supervisor', 'OperadorPatio'],
    subs: [{ rota: '/cadastro', rotulo: 'Cadastrar peça', curto: 'Cadastrar' }, { rota: '/cadastro/andamento', rotulo: 'Andamento da carga', curto: 'Andamento' }],
  },
  {
    rota: '/qualidade', rotulo: 'Verificação de qualidade', grupo: 'Peça', cargos: ['Supervisor', 'Especialista'],
    subs: [{ rota: '/qualidade', rotulo: 'Verificar a peça', curto: 'Verificar' }, { rota: '/qualidade/cargas', rotulo: 'Cargas e boletins', curto: 'Cargas' }],
  },
  {
    rota: '/armazem', rotulo: 'Armazém', grupo: 'Peça', cargos: ['Supervisor', 'OperadorPatio', 'Especialista'],
    subs: [
      { rota: '/armazem', rotulo: 'Guardar na pilha', curto: 'Guardar', cargos: ['Supervisor', 'OperadorPatio'] },
      { rota: '/armazem/pilhas', rotulo: 'Pilhas dos armazéns', curto: 'Pilhas' },
      { rota: '/armazem/estoque', rotulo: 'Estoque', curto: 'Estoque', soComputador: true },
      { rota: '/armazem/conferencia', rotulo: 'Conferência', curto: 'Conferir' },
    ],
  },
  { rota: '/pecas', rotulo: 'Consultar peça', grupo: 'Peça', cargos: todos },
  { rota: '/alertas', rotulo: 'Alertas', grupo: 'Campo', cargos: ['Supervisor', 'Tecnico', 'OperadorVia'] },
  { rota: '/inspecoes', rotulo: 'Inspeção', grupo: 'Campo', cargos: ['Tecnico'] },
  { rota: '/trocas', rotulo: 'Trocas', grupo: 'Campo', cargos: todos },
  { rota: '/indicadores', rotulo: 'Indicadores', grupo: 'Supervisão', cargos: todos },
  { rota: '/relatorio-ambiental', rotulo: 'Relatório ambiental', grupo: 'Supervisão', cargos: ['Supervisor', 'Especialista'] },
  { rota: '/cadastros', rotulo: 'Cadastros', grupo: 'Supervisão', cargos: ['Supervisor'] },
  {
    rota: '/usuarios', rotulo: 'Usuários', grupo: 'Supervisão', cargos: ['Supervisor'],
    subs: [{ rota: '/usuarios/novo', rotulo: 'Cadastrar usuário', curto: 'Cadastrar', soComputador: true }, { rota: '/usuarios', rotulo: 'Usuários cadastrados', curto: 'Usuários' }],
  },
];

export const menuDoCargo = (cargo: Cargo | null | undefined): ItemMenu[] => (cargo ? MENU.filter((i) => i.cargos.includes(cargo)) : []);