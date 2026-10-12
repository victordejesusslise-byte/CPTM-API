import { inject } from '@angular/core';
import { Router, Routes } from '@angular/router';
import { cargoGuard } from './core/auth/cargo-guard';
import { Cargo } from './models/usuario';

const so = (...cargos: Cargo[]) => ({ cargos });
const titulo = (nome: string) => `${nome} · YBYRA`;

/** As rotas: o login fica fora da casca; o resto pede login e, quando tem data.cargos, o cargo certo. Cada tela carrega só quando abre. */
export const routes: Routes = [
  { path: 'login', title: titulo('Entrar'), loadComponent: () => import('./features/login/login').then((m) => m.Login) },
  {
    path: '',
    canActivate: [cargoGuard],
    canActivateChild: [cargoGuard],
    loadComponent: () => import('./shared/casca/casca').then((m) => m.Casca),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'painel' },
      { path: 'painel', title: titulo('O que fazer hoje'), data: { aba: 'hoje' }, loadComponent: () => import('./features/painel/painel').then((m) => m.Painel) },
      { path: 'painel/numeros', title: titulo('Acompanhamento'), data: { aba: 'numeros' }, loadComponent: () => import('./features/painel/painel').then((m) => m.Painel) },
      { path: 'pedidos', title: titulo('Acompanhar pedidos'), data: so('Supervisor', 'OperadorPatio'), loadComponent: () => import('./features/pedidos/acompanhar-pedidos').then((m) => m.AcompanharPedidos) },
      { path: 'pedidos/novo', title: titulo('Cadastrar pedido'), data: so('Supervisor', 'OperadorPatio'), loadComponent: () => import('./features/pedidos/cadastrar-pedido').then((m) => m.CadastrarPedido) },
      { path: 'pedidos/:id', redirectTo: ({ params }) => inject(Router).createUrlTree(['/pedidos'], { queryParams: { aberto: params['id'] } }) },
      { path: 'chegadas', title: titulo('Chegada da carga'), data: so('Supervisor', 'OperadorPatio'), loadComponent: () => import('./features/chegada/chegada').then((m) => m.Chegada) },
      { path: 'chegadas/entregas', title: titulo('Entregas recebidas'), data: { ...so('Supervisor', 'OperadorPatio'), modo: 'entregas' }, loadComponent: () => import('./features/chegada/lista-chegadas').then((m) => m.ListaChegadas) },
      { path: 'cadastro', title: titulo('Cadastro da peça'), data: so('Supervisor', 'OperadorPatio'), loadComponent: () => import('./features/cadastro/cadastro').then((m) => m.Cadastro) },
      { path: 'cadastro/andamento', title: titulo('Andamento da carga'), data: { ...so('Supervisor', 'OperadorPatio'), modo: 'andamento' }, loadComponent: () => import('./features/chegada/lista-chegadas').then((m) => m.ListaChegadas) },
      { path: 'qualidade', title: titulo('Qualidade'), data: so('Supervisor', 'Especialista'), loadComponent: () => import('./features/qualidade/qualidade').then((m) => m.Qualidade) },
      { path: 'qualidade/cargas', title: titulo('Cargas e boletins'), data: { ...so('Supervisor', 'Especialista'), modo: 'boletins' }, loadComponent: () => import('./features/chegada/lista-chegadas').then((m) => m.ListaChegadas) },
      { path: 'armazem', title: titulo('Guardar na pilha'), data: { ...so('Supervisor', 'OperadorPatio', 'Especialista'), aba: 'guardar' }, loadComponent: () => import('./features/armazem/armazem').then((m) => m.Armazem) },
      { path: 'armazem/pilhas', title: titulo('Pilhas dos armazéns'), data: { ...so('Supervisor', 'OperadorPatio', 'Especialista'), aba: 'pilhas' }, loadComponent: () => import('./features/armazem/armazem').then((m) => m.Armazem) },
      { path: 'armazem/estoque', title: titulo('Estoque'), data: { ...so('Supervisor', 'OperadorPatio', 'Especialista'), aba: 'estoque' }, loadComponent: () => import('./features/armazem/armazem').then((m) => m.Armazem) },
      { path: 'armazem/conferencia', title: titulo('Conferência'), data: { ...so('Supervisor', 'OperadorPatio', 'Especialista'), aba: 'conferencia' }, loadComponent: () => import('./features/armazem/armazem').then((m) => m.Armazem) },
      { path: 'pecas', title: titulo('Consultar peça'), loadComponent: () => import('./features/pecas/consulta-peca').then((m) => m.ConsultaPeca) },
      { path: 'alertas', title: titulo('Alertas da câmera'), data: { ...so('Supervisor', 'Tecnico', 'OperadorVia'), aba: 'imagem' }, loadComponent: () => import('./features/alertas/alertas').then((m) => m.Alertas) },
      { path: 'alertas/sensores', title: titulo('Alertas dos sensores'), data: { ...so('Supervisor', 'Tecnico', 'OperadorVia'), aba: 'sensores' }, loadComponent: () => import('./features/alertas/alertas').then((m) => m.Alertas) },
      { path: 'alertas/tags', title: titulo('Tags sem cadastro'), data: { ...so('Tecnico', 'OperadorVia'), aba: 'tags' }, loadComponent: () => import('./features/alertas/alertas').then((m) => m.Alertas) },
      { path: 'trocas-trecho', title: titulo('A trocar'), data: { ...so('Supervisor', 'OperadorVia'), aba: 'trocar' }, loadComponent: () => import('./features/trocas/trocas-trecho').then((m) => m.TrocasTrecho) },
      { path: 'trocas-trecho/recusas', title: titulo('Recusas enviadas'), data: { ...so('Supervisor', 'OperadorVia'), aba: 'recusas' }, loadComponent: () => import('./features/trocas/trocas-trecho').then((m) => m.TrocasTrecho) },
      { path: 'trocas-trecho/feitas', title: titulo('Trocas feitas'), data: { ...so('Supervisor', 'OperadorVia'), aba: 'feitas' }, loadComponent: () => import('./features/trocas/trocas-trecho').then((m) => m.TrocasTrecho) },
      { path: 'separar', title: titulo('Trocas para separar'), data: { ...so('Supervisor', 'OperadorPatio'), aba: 'fila' }, loadComponent: () => import('./features/trocas/separar-peca').then((m) => m.SepararPeca) },
      { path: 'separar/separadas', title: titulo('Separadas'), data: { ...so('Supervisor', 'OperadorPatio'), aba: 'separadas' }, loadComponent: () => import('./features/trocas/separar-peca').then((m) => m.SepararPeca) },
      { path: 'usuarios', title: titulo('Usuários cadastrados'), data: so('Supervisor'), loadComponent: () => import('./features/usuarios/lista-usuarios').then((m) => m.ListaUsuarios) },
      { path: 'usuarios/novo', title: titulo('Cadastrar usuário'), data: so('Supervisor'), loadComponent: () => import('./features/usuarios/cadastrar-usuario').then((m) => m.CadastrarUsuario) },
    ],
  },
  { path: '**', redirectTo: '' },
];