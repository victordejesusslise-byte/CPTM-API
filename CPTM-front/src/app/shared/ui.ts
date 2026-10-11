import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';

/**
 * Os embrulhos do Angular Material (MIT): as telas importam daqui, nunca direto da biblioteca.
 * O PrimeNG saiu em 2026-10-11 (virou licença paga; decisão do Victor). As tabelas ficam em shared/tabela.
 */
export const FORMULARIO = [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatCheckboxModule] as const;
export const CARREGANDO = [MatProgressBarModule] as const;
export const ABAS = [MatTabsModule] as const;