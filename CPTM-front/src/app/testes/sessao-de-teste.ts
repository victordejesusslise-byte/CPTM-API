import { TokenDto, UsuarioDto } from '../models/usuario';

/** Uma sessão pronta para os testes: grava no armazenamento como o login faria. */
export const usuarioDeTeste = (cargo: UsuarioDto['cargo'] = 'Supervisor'): UsuarioDto => ({
  id: 1, nome: 'Ana Souza', email: 'ana@cptm.sp.gov.br', cargo, ativo: true,
});

export const tokenDeTeste = (cargo: UsuarioDto['cargo'] = 'Supervisor', minutos = 60): TokenDto => ({
  token: 'abc.def.ghi',
  expiraEm: new Date(Date.now() + minutos * 60_000).toISOString(),
  usuario: usuarioDeTeste(cargo),
});

export const entrarComo = (cargo: UsuarioDto['cargo'] = 'Supervisor', minutos = 60): void =>
  localStorage.setItem('via-permanente.sessao', JSON.stringify(tokenDeTeste(cargo, minutos)));
