import { Component, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, debounceTime, of, switchMap } from 'rxjs';
import { mensagemDeErro, metros } from '../../helpers/formato';
import { NovoPedidoItemDto, VerificacaoPedidoDto } from '../../models/pedido';
import { Cadastros } from '../../services/cadastros';
import { Catalogos } from '../../services/catalogos';
import { Pedidos } from '../../services/pedidos';
import { FORMULARIO } from '../../shared/ui';

/** Registrar o pedido: a cada mudança a API confere a 2ª classe (bloqueia) e a oferta do fornecedor (só avisa). */
@Component({
  selector: 'app-novo-pedido',
  imports: [RouterLink, FORMULARIO],
  templateUrl: './novo-pedido.html',
})
export class NovoPedido {
  private readonly fb = inject(FormBuilder).nonNullable;
  private readonly pedidos = inject(Pedidos);
  private readonly cadastros = inject(Cadastros);
  private readonly router = inject(Router);

  protected readonly metros = metros;
  protected readonly fornecedores = rxResource({ stream: () => this.cadastros.empresas('Fornecedor') });
  protected readonly tamanhos = toSignal(inject(Catalogos).tamanhos(), { initialValue: [] });
  protected readonly especies = toSignal(inject(Catalogos).especies(), { initialValue: [] });

  protected readonly form = this.fb.group({
    codigo: ['', Validators.required],
    idEmpresa: [0, Validators.min(1)],
    idContrato: [null as number | null],
    dataCompra: [''],
    itens: this.fb.array([this.novoItem()]),
  });
  protected readonly idEmpresa = toSignal(this.form.controls.idEmpresa.valueChanges, { initialValue: 0 });
  protected readonly contratos = rxResource({
    params: () => this.idEmpresa() || undefined,
    stream: ({ params }) => this.cadastros.contratos(params),
  });

  protected readonly verificacao = signal<VerificacaoPedidoDto | null>(null);
  protected readonly bloqueado = computed(() => this.verificacao()?.segundaClasse.valido === false);
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  constructor() {
    this.form.valueChanges
      .pipe(
        debounceTime(400),
        switchMap(() => {
          const itens = this.itensValidos();
          return itens.length ? this.pedidos.verificar(this.form.controls.idEmpresa.value || null, itens).pipe(catchError(() => of(null))) : of(null);
        }),
        takeUntilDestroyed(),
      )
      .subscribe((v) => this.verificacao.set(v));
  }

  protected get itens(): FormArray {
    return this.form.controls.itens;
  }

  protected novoItem() {
    return this.fb.group({
      idTamanho: [0, Validators.min(1)],
      classe: [1, Validators.required],
      idEspecie: [null as number | null],
      quantidade: [1, [Validators.required, Validators.min(1)]],
    });
  }

  protected adicionarItem(): void {
    this.itens.push(this.novoItem());
  }

  protected removerItem(i: number): void {
    if (this.itens.length > 1) this.itens.removeAt(i);
  }

  /** A espécie, se vier, manda na classe. */
  protected especieMudou(i: number, idEspecie: number | null): void {
    const especie = this.especies().find((e) => e.id === idEspecie);
    if (especie) this.itens.at(i).patchValue({ classe: especie.classe });
  }

  private itensValidos(): NovoPedidoItemDto[] {
    return (this.form.getRawValue().itens as NovoPedidoItemDto[]).filter((i) => i.idTamanho > 0 && i.quantidade > 0);
  }

  protected salvar(): void {
    if (this.form.invalid || this.bloqueado()) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.enviando.set(true);
    this.erro.set(null);
    this.pedidos
      .criar({ codigo: v.codigo.trim(), idEmpresa: v.idEmpresa, idContrato: v.idContrato, dataCompra: v.dataCompra || null, itens: this.itensValidos() })
      .subscribe({
        next: (p) => this.router.navigate(['/pedidos', p.id]),
        error: (e) => {
          this.erro.set(mensagemDeErro(e));
          this.enviando.set(false);
        },
      });
  }
}