import { TokenDto, UsuarioDto } from '../../pessoas/entidades/usuario';

/** Uma sessão pronta para os testes: grava no armazenamento como o login faria. */
export const usuarioDeTeste = (cargo: UsuarioDto['cargo'] = 'Supervisor'): UsuarioDto => ({
  id: 1, nome: 'Ana Souza', email: 'ana@cptm.sp.gov.br', cargo, ativo: true,
});

const base64Url = (texto: string) =>
  btoa(String.fromCharCode(...new TextEncoder().encode(texto))).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

/** Um JWT de mentira, com o cargo na carga como o da API (a assinatura não é conferida no front). */
export const jwtDeTeste = (usuario: UsuarioDto = usuarioDeTeste()): string =>
  [
    base64Url(JSON.stringify({ alg: 'HS256', typ: 'JWT' })),
    base64Url(JSON.stringify({ sub: String(usuario.id), name: usuario.nome, email: usuario.email, role: usuario.cargo })),
    'assinatura-de-teste',
  ].join('.');

export const tokenDeTeste = (cargo: UsuarioDto['cargo'] = 'Supervisor', minutos = 60): TokenDto => ({
  token: jwtDeTeste(usuarioDeTeste(cargo)),
  expiraEm: new Date(Date.now() + minutos * 60_000).toISOString(),
  usuario: usuarioDeTeste(cargo),
});

export const entrarComo = (cargo: UsuarioDto['cargo'] = 'Supervisor', minutos = 60): void =>
  localStorage.setItem('via-permanente.sessao', JSON.stringify(tokenDeTeste(cargo, minutos)));
