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
      { path: 'painel', title: titulo('Painel'), loadComponent: () => import('./features/painel/painel').then((m) => m.Painel) },
      { path: 'pedidos', title: titulo('Acompanhar pedidos'), data: so('Supervisor', 'OperadorPatio'), loadComponent: () => import('./features/pedidos/acompanhar-pedidos').then((m) => m.AcompanharPedidos) },
      { path: 'pedidos/novo', title: titulo('Cadastrar pedido'), data: so('Supervisor', 'OperadorPatio'), loadComponent: () => import('./features/pedidos/cadastrar-pedido').then((m) => m.CadastrarPedido) },
      { path: 'pedidos/:id', redirectTo: ({ params }) => inject(Router).createUrlTree(['/pedidos'], { queryParams: { aberto: params['id'] } }) },
      { path: 'chegadas', title: titulo('Chegada da carga'), data: so('Supervisor', 'OperadorPatio'), loadComponent: () => import('./features/chegada/chegada').then((m) => m.Chegada) },
      { path: 'cadastro', title: titulo('Cadastro da peça'), data: so('Supervisor', 'OperadorPatio'), loadComponent: () => import('./features/cadastro/cadastro').then((m) => m.Cadastro) },
      { path: 'qualidade', title: titulo('Qualidade'), data: so('Supervisor', 'Especialista'), loadComponent: () => import('./features/qualidade/qualidade').then((m) => m.Qualidade) },
      { path: 'armazem', title: titulo('Armazém'), data: so('Supervisor', 'OperadorPatio', 'Especialista'), loadComponent: () => import('./features/armazem/armazem').then((m) => m.Armazem) },
      { path: 'pecas', title: titulo('Consultar peça'), loadComponent: () => import('./features/pecas/consulta-peca').then((m) => m.ConsultaPeca) },
    ],
  },
  { path: '**', redirectTo: '' },
];