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
    subs: [
      { rota: '/chegadas/placa', rotulo: 'Placa do feixe', curto: 'Placa' },
      { rota: '/chegadas', rotulo: 'Registrar chegada', curto: 'Registrar' },
      { rota: '/chegadas/entregas', rotulo: 'Entregas recebidas', curto: 'Entregas' },
    ],
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
  {
    rota: '/separar', rotulo: 'Separar peça', grupo: 'Peça', cargos: ['Supervisor', 'OperadorPatio'],
    subs: [
      { rota: '/separar', rotulo: 'Trocas para separar', curto: 'Separar' },
      { rota: '/separar/separadas', rotulo: 'Separadas, esperando a via', curto: 'Separadas' },
    ],
  },
  { rota: '/pecas', rotulo: 'Consultar peça', grupo: 'Peça', cargos: todos },
  {
    rota: '/alertas', rotulo: 'Alertas', grupo: 'Campo', cargos: ['Supervisor', 'Tecnico', 'OperadorVia'],
    subs: [
      { rota: '/alertas', rotulo: 'Alertas da câmera', curto: 'Câmera' },
      { rota: '/alertas/sensores', rotulo: 'Alertas dos sensores', curto: 'Sensores' },
      { rota: '/alertas/tags', rotulo: 'Tags sem cadastro', curto: 'Tags', cargos: ['OperadorVia', 'Tecnico'] },
    ],
  },
  {
    rota: '/trocas-trecho', rotulo: 'Trocas do trecho', grupo: 'Campo', cargos: ['Supervisor', 'OperadorVia'],
    subs: [
      { rota: '/trocas-trecho', rotulo: 'A trocar', curto: 'A trocar' },
      { rota: '/trocas-trecho/recusas', rotulo: 'Recusas enviadas ao técnico', curto: 'Recusas' },
      { rota: '/trocas-trecho/feitas', rotulo: 'Trocas feitas', curto: 'Feitas' },
    ],
  },
  {
    rota: '/inspecoes', rotulo: 'Inspeção', grupo: 'Campo', cargos: ['Tecnico'],
    subs: [{ rota: '/inspecoes', rotulo: 'Fazer a inspeção', curto: 'Inspecionar' }, { rota: '/inspecoes/feitas', rotulo: 'Inspeções feitas', curto: 'Feitas' }],
  },
  {
    rota: '/aprovar', rotulo: 'Aprovar trocas', grupo: 'Campo', cargos: ['Supervisor'],
    subs: [{ rota: '/aprovar', rotulo: 'Para aprovar', curto: 'Aprovar' }, { rota: '/aprovar/decididas', rotulo: 'Decididas', curto: 'Decididas' }],
  },
  {
    rota: '/trocas', rotulo: 'Trocas e laudos', grupo: 'Campo', cargos: ['Supervisor', 'Tecnico'],
    subs: [
      { rota: '/trocas', rotulo: 'Em andamento', curto: 'Andamento' },
      { rota: '/trocas/recusas', rotulo: 'Recusas da via', curto: 'Recusas' },
      { rota: '/trocas/feitas', rotulo: 'Trocas feitas', curto: 'Feitas' },
    ],
  },
  { rota: '/indicadores', rotulo: 'Indicadores', grupo: 'Supervisão', cargos: todos },
  {
    rota: '/relatorio-ambiental', rotulo: 'Relatório ambiental', grupo: 'Supervisão', cargos: ['Supervisor', 'Especialista'],
    subs: [
      { rota: '/relatorio-ambiental', rotulo: 'Relatório do mês', curto: 'Mês' },
      { rota: '/relatorio-ambiental/especies', rotulo: 'Por espécie', curto: 'Espécies' },
    ],
  },
  {
    rota: '/cadastros', rotulo: 'Cadastros', grupo: 'Supervisão', cargos: ['Supervisor'],
    subs: [
      { rota: '/cadastros', rotulo: 'Empresas', curto: 'Empresas' },
      { rota: '/cadastros/contratos', rotulo: 'Contratos', curto: 'Contratos' },
      { rota: '/cadastros/via', rotulo: 'Estações e trechos', curto: 'Via' },
      { rota: '/cadastros/parametros', rotulo: 'Parâmetros', curto: 'Parâmetros' },
      { rota: '/cadastros/leitores', rotulo: 'Leitores de tag', curto: 'Leitores' },
    ],
  },
  {
    rota: '/integracoes', rotulo: 'Integrações', grupo: 'Supervisão', cargos: ['Supervisor', 'OperadorPatio'],
    subs: [
      { rota: '/integracoes', rotulo: 'Notas pela planilha', curto: 'Notas' },
      { rota: '/integracoes/erp', rotulo: 'Estoque para o ERP', curto: 'ERP' },
      { rota: '/integracoes/historico', rotulo: 'Histórico', curto: 'Histórico' },
    ],
  },
  {
    rota: '/usuarios', rotulo: 'Usuários', grupo: 'Supervisão', cargos: ['Supervisor'],
    subs: [{ rota: '/usuarios/novo', rotulo: 'Cadastrar usuário', curto: 'Cadastrar', soComputador: true }, { rota: '/usuarios', rotulo: 'Usuários cadastrados', curto: 'Usuários' }],
  },
];

export const menuDoCargo = (cargo: Cargo | null | undefined): ItemMenu[] => (cargo ? MENU.filter((i) => i.cargos.includes(cargo)) : []);