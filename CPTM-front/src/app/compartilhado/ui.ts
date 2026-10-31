import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { Carregando } from './componentes/esqueleto/carregando';
import { BuscaNasOpcoes } from './componentes/busca-opcoes/busca-opcoes';

/**
 * Os embrulhos do Angular Material (MIT): as telas importam daqui, nunca direto da biblioteca.
 * O PrimeNG saiu em 2026-10-11 (virou licença paga; decisão do Victor). As tabelas ficam em compartilhado/componentes/tabela.
 */
/** Com a pesquisa nas listas dos selects (<app-busca-opcoes />, 2026-10-31). */
export const FORMULARIO = [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatCheckboxModule, Carregando, BuscaNasOpcoes] as const;
export const CARREGANDO = [MatProgressBarModule] as const;
export const ABAS = [MatTabsModule] as const;export const BOTOES = [MatButtonModule, Carregando] as const;
