/** Os cinco cargos do back (Cargo.cs). Viram as permissões do token. */
export type Cargo = 'Supervisor' | 'Tecnico' | 'Especialista' | 'OperadorPatio' | 'OperadorVia';

export interface UsuarioDto {
  id: number;
  nome: string;
  email: string;
  cargo: Cargo;
  ativo: boolean;
}

export interface TokenDto {
  token: string;
  expiraEm: string;
  usuario: UsuarioDto;
}

export interface NovoUsuarioDto {
  nome: string;
  email: string;
  senha: string;
  cargo: Cargo;
}