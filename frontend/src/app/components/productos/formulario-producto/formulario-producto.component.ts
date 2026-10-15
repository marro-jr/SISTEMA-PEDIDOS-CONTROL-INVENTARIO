import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProductosService } from '../../../services/productos.service';

@Component({
  selector: 'app-formulario-producto',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  template: `
    <div class="container py-4">
      <div class="row justify-content-center">
        <div class="col-12 col-md-8 col-lg-6">
          <div class="card shadow-sm">
            <div class="card-header bg-white py-3">
              <h4 class="fw-bold mb-0">
                <i class="bi" [ngClass]="esEdicion ? 'bi-pencil-square text-primary' : 'bi-plus-circle text-success'"></i>
                {{ esEdicion ? 'Editar Producto #' + productoId : 'Registrar Nuevo Producto' }}
              </h4>
              <p class="text-muted small mb-0 mt-1">
                {{ esEdicion ? 'Modifique los campos necesarios y guarde los cambios.' : 'Complete la información para añadir un producto al catálogo.' }}
              </p>
            </div>
            <div class="card-body p-4">
              <!-- Mensaje de error -->
              <div *ngIf="mensajeError" class="alert alert-danger d-flex align-items-center gap-2 mb-3" role="alert">
                <i class="bi bi-exclamation-triangle-fill fs-5"></i>
                <div>{{ mensajeError }}</div>
              </div>

              <form [formGroup]="formulario" (ngSubmit)="guardarProducto()">
                <!-- Nombre -->
                <div class="mb-3">
                  <label for="name" class="form-label fw-semibold">Nombre del Producto <span class="text-danger">*</span></label>
                  <input
                    type="text"
                    id="name"
                    class="form-control"
                    formControlName="name"
                    placeholder="Ej. Monitor LED 27 Pulgadas"
                    [ngClass]="{ 'is-invalid': campoInvalido('name') }"
                  />
                  <div class="invalid-feedback" *ngIf="campoInvalido('name')">
                    El nombre es obligatorio y debe tener máximo 150 caracteres.
                  </div>
                </div>

                <!-- Descripción -->
                <div class="mb-3">
                  <label for="description" class="form-label fw-semibold">Descripción</label>
                  <textarea
                    id="description"
                    rows="3"
                    class="form-control"
                    formControlName="description"
                    placeholder="Detalles técnicos, dimensiones, especificaciones..."
                    [ngClass]="{ 'is-invalid': campoInvalido('description') }"
                  ></textarea>
                  <div class="invalid-feedback" *ngIf="campoInvalido('description')">
                    La descripción no puede exceder los 500 caracteres.
                  </div>
                </div>

                <div class="row g-3 mb-3">
                  <!-- Precio -->
                  <div class="col-6">
                    <label for="price" class="form-label fw-semibold">Precio (S/) <span class="text-danger">*</span></label>
                    <div class="input-group">
                      <span class="input-group-text">S/</span>
                      <input
                        type="number"
                        id="price"
                        step="0.01"
                        min="0.01"
                        class="form-control"
                        formControlName="price"
                        placeholder="0.00"
                        [ngClass]="{ 'is-invalid': campoInvalido('price') }"
                      />
                    </div>
                    <div class="invalid-feedback d-block" *ngIf="campoInvalido('price')">
                      El precio debe ser mayor a cero.
                    </div>
                  </div>

                  <!-- Stock -->
                  <div class="col-6">
                    <label for="stock" class="form-label fw-semibold">Existencias (Stock) <span class="text-danger">*</span></label>
                    <input
                      type="number"
                      id="stock"
                      min="0"
                      class="form-control"
                      formControlName="stock"
                      placeholder="0"
                      [ngClass]="{ 'is-invalid': campoInvalido('stock') }"
                    />
                    <div class="invalid-feedback" *ngIf="campoInvalido('stock')">
                      El stock no puede ser un valor negativo.
                    </div>
                  </div>
                </div>

                <!-- Estado activo (solo en edición) -->
                <div *ngIf="esEdicion" class="mb-4 form-check form-switch">
                  <input
                    type="checkbox"
                    class="form-check-input"
                    id="active"
                    formControlName="active"
                  />
                  <label class="form-check-label fw-semibold" for="active">
                    Producto Activo para Venta
                  </label>
                  <div class="form-text">
                    Si se desactiva, los usuarios no podrán agregarlo a nuevos pedidos.
                  </div>
                </div>

                <!-- Botones -->
                <div class="d-flex justify-content-end gap-2 mt-4 pt-2 border-top">
                  <a routerLink="/productos" class="btn btn-outline-secondary">
                    Cancelar
                  </a>
                  <button
                    type="submit"
                    class="btn btn-primary d-flex align-items-center gap-2"
                    [disabled]="guardando || formulario.invalid"
                  >
                    <span *ngIf="guardando" class="spinner-border spinner-border-sm" role="status"></span>
                    <i *ngIf="!guardando" class="bi bi-save"></i>
                    {{ esEdicion ? 'Actualizar Producto' : 'Guardar Producto' }}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class FormularioProductoComponent implements OnInit {
  formulario!: FormGroup;
  esEdicion = false;
  productoId = 0;
  guardando = false;
  mensajeError = '';

  constructor(
    private fb: FormBuilder,
    private productosService: ProductosService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.inicializarFormulario();

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.esEdicion = true;
      this.productoId = Number(idParam);
      this.cargarProducto(this.productoId);
    }
  }

  inicializarFormulario(): void {
    this.formulario = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      description: ['', [Validators.maxLength(500)]],
      price: [null, [Validators.required, Validators.min(0.01)]],
      stock: [0, [Validators.required, Validators.min(0)]],
      active: [true]
    });
  }

  cargarProducto(id: number): void {
    this.productosService.obtenerProductoPorId(id).subscribe({
      next: (producto) => {
        this.formulario.patchValue({
          name: producto.name,
          description: producto.description,
          price: producto.price,
          stock: producto.stock,
          active: producto.active
        });
      },
      error: (err) => {
        this.mensajeError = 'No se pudo cargar la información del producto.';
      }
    });
  }

  campoInvalido(nombreCampo: string): boolean {
    const control = this.formulario.get(nombreCampo);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  guardarProducto(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    this.guardando = true;
    this.mensajeError = '';

    const valores = this.formulario.value;

    if (this.esEdicion) {
      this.productosService.actualizarProducto(this.productoId, valores).subscribe({
        next: () => {
          this.guardando = false;
          this.router.navigate(['/productos']);
        },
        error: (err) => {
          this.mensajeError = err.error?.detail || 'Error al actualizar el producto.';
          this.guardando = false;
        }
      });
    } else {
      this.productosService.crearProducto(valores).subscribe({
        next: () => {
          this.guardando = false;
          this.router.navigate(['/productos']);
        },
        error: (err) => {
          this.mensajeError = err.error?.detail || 'Error al crear el producto.';
          this.guardando = false;
        }
      });
    }
  }
}
