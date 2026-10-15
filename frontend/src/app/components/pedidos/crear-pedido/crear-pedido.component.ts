import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductosService } from '../../../services/productos.service';
import { PedidosService } from '../../../services/pedidos.service';
import { Producto } from '../../../models/producto.model';
import { CrearPedido, ItemPedido } from '../../../models/pedido.model';

interface ItemCanasta {
  producto: Producto;
  quantity: number;
  subtotal: number;
}

@Component({
  selector: 'app-crear-pedido',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="container py-4">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
          <a routerLink="/pedidos" class="btn btn-outline-secondary btn-sm mb-2">
            <i class="bi bi-arrow-left"></i> Volver a Historial
          </a>
          <h2 class="fw-bold mb-0">Registrar Nuevo Pedido</h2>
          <p class="text-muted small mb-0">Seleccione los productos y cantidades a incluir en la transacción.</p>
        </div>
      </div>

      <!-- Alertas de error del servidor -->
      <div *ngIf="mensajeError" class="alert alert-danger d-flex align-items-center gap-2 mb-4" role="alert">
        <i class="bi bi-exclamation-triangle-fill fs-5"></i>
        <div>
          <strong>Error en la operación:</strong> {{ mensajeError }}
        </div>
      </div>

      <div class="row g-4">
        <!-- Formulario selector de producto -->
        <div class="col-12 col-lg-5">
          <div class="card shadow-sm">
            <div class="card-header bg-white py-3">
              <h5 class="fw-bold mb-0">
                <i class="bi bi-cart-plus text-primary me-2"></i>Agregar Producto
              </h5>
            </div>
            <div class="card-body">
              <div *ngIf="cargandoProductos" class="text-center py-4 text-muted">
                <div class="spinner-border spinner-border-sm text-primary me-2"></div>
                Cargando catálogo activo...
              </div>

              <div *ngIf="!cargandoProductos">
                <!-- Selector de producto -->
                <div class="mb-3">
                  <label class="form-label fw-semibold">Seleccionar Producto</label>
                  <select
                    class="form-select"
                    [(ngModel)]="productoSeleccionadoId"
                    (change)="alSeleccionarProducto()"
                  >
                    <option [ngValue]="null">-- Elija un producto --</option>
                    <option *ngFor="let prod of productosDisponibles" [ngValue]="prod.id">
                      {{ prod.name }} (Stock: {{ prod.stock }} | S/ {{ prod.price | number:'1.2-2' }})
                    </option>
                  </select>
                </div>

                <!-- Detalle del producto seleccionado -->
                <div *ngIf="productoSeleccionado" class="p-3 bg-light rounded mb-3 border">
                  <div class="d-flex justify-content-between mb-1">
                    <span class="text-muted">Precio Unitario:</span>
                    <span class="fw-bold">S/ {{ productoSeleccionado.price | number:'1.2-2' }}</span>
                  </div>
                  <div class="d-flex justify-content-between mb-1">
                    <span class="text-muted">Existencias Disponibles:</span>
                    <span class="badge" [ngClass]="productoSeleccionado.stock <= 5 ? 'bg-danger' : 'bg-success'">
                      {{ productoSeleccionado.stock }} unid.
                    </span>
                  </div>
                  <div *ngIf="productoSeleccionado.description" class="small text-muted mt-2 border-top pt-2">
                    {{ productoSeleccionado.description }}
                  </div>
                </div>

                <!-- Cantidad -->
                <div class="mb-3">
                  <label class="form-label fw-semibold">Cantidad</label>
                  <input
                    type="number"
                    class="form-control"
                    [(ngModel)]="cantidadIngresada"
                    min="1"
                    [max]="productoSeleccionado ? productoSeleccionado.stock : 9999"
                    [disabled]="!productoSeleccionado || productoSeleccionado.stock <= 0"
                  />
                  <div *ngIf="productoSeleccionado && cantidadIngresada > productoSeleccionado.stock" class="text-danger small mt-1">
                    La cantidad solicitada supera el stock disponible ({{ productoSeleccionado.stock }}).
                  </div>
                </div>

                <button
                  type="button"
                  class="btn btn-outline-primary w-100 d-flex align-items-center justify-content-center gap-2"
                  [disabled]="!puedeAgregarItem()"
                  (click)="agregarItem()"
                >
                  <i class="bi bi-plus-lg"></i> Agregar al Pedido
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Canasta / Detalle del pedido actual -->
        <div class="col-12 col-lg-7">
          <div class="card shadow-sm">
            <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
              <h5 class="fw-bold mb-0">
                <i class="bi bi-bag-check text-success me-2"></i>Artículos del Pedido ({{ canasta.length }})
              </h5>
              <button
                *ngIf="canasta.length > 0"
                type="button"
                class="btn btn-sm btn-outline-danger"
                (click)="limpiarCanasta()"
              >
                Vaciar
              </button>
            </div>
            <div class="card-body p-0">
              <div *ngIf="canasta.length === 0" class="text-center py-5 text-muted">
                <i class="bi bi-cart-x fs-1"></i>
                <h6 class="mt-2">El pedido aún no tiene artículos</h6>
                <p class="small mb-0">Seleccione un producto en el formulario lateral y presione "Agregar al Pedido".</p>
              </div>

              <div *ngIf="canasta.length > 0" class="table-responsive">
                <table class="table table-hover mb-0 align-middle">
                  <thead class="table-light">
                    <tr>
                      <th>Producto</th>
                      <th class="text-center" style="width: 130px;">Cantidad</th>
                      <th class="text-end" style="width: 120px;">Precio</th>
                      <th class="text-end" style="width: 130px;">Subtotal</th>
                      <th class="text-center" style="width: 60px;"></th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let item of canasta; let i = index">
                      <td>
                        <div class="fw-semibold">{{ item.producto.name }}</div>
                        <small class="text-muted">Stock actual: {{ item.producto.stock }}</small>
                      </td>
                      <td class="text-center">
                        <input
                          type="number"
                          class="form-control form-control-sm text-center"
                          [(ngModel)]="item.quantity"
                          min="1"
                          [max]="item.producto.stock"
                          (input)="recalcularSubtotal(item)"
                        />
                      </td>
                      <td class="text-end">S/ {{ item.producto.price | number:'1.2-2' }}</td>
                      <td class="text-end fw-bold">S/ {{ item.subtotal | number:'1.2-2' }}</td>
                      <td class="text-center">
                        <button
                          type="button"
                          class="btn btn-sm text-danger"
                          (click)="eliminarItem(i)"
                          title="Eliminar ítem"
                        >
                          <i class="bi bi-trash"></i>
                        </button>
                      </td>
                    </tr>
                  </tbody>
                  <tfoot class="table-light">
                    <tr>
                      <td colspan="3" class="text-end fw-bold fs-6">Total Estimado:</td>
                      <td class="text-end fw-bold fs-5 text-success">S/ {{ totalEstimado | number:'1.2-2' }}</td>
                      <td></td>
                    </tr>
                  </tfoot>
                </table>
              </div>

              <div class="p-3 bg-light border-top">
                <div class="small text-muted mb-3">
                  <i class="bi bi-shield-check text-primary me-1"></i>
                  El backend recalculará los subtotales, congelará el precio unitario y descontará el stock dentro de una transacción atómica protegida contra concurrencia.
                </div>

                <div class="d-flex justify-content-end gap-2">
                  <a routerLink="/pedidos" class="btn btn-outline-secondary">Cancelar</a>
                  <button
                    type="button"
                    class="btn btn-success d-flex align-items-center gap-2"
                    [disabled]="canasta.length === 0 || procesando"
                    (click)="confirmarPedido()"
                  >
                    <span *ngIf="procesando" class="spinner-border spinner-border-sm" role="status"></span>
                    <i *ngIf="!procesando" class="bi bi-check-circle"></i>
                    Confirmar y Registrar Pedido
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class CrearPedidoComponent implements OnInit {
  productosDisponibles: Producto[] = [];
  cargandoProductos = true;
  productoSeleccionadoId: number | null = null;
  productoSeleccionado: Producto | null = null;
  cantidadIngresada = 1;

  canasta: ItemCanasta[] = [];
  procesando = false;
  mensajeError = '';

  constructor(
    private productosService: ProductosService,
    private pedidosService: PedidosService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.cargarCatalogo();
  }

  cargarCatalogo(): void {
    this.cargandoProductos = true;
    this.productosService.obtenerProductos(true).subscribe({
      next: (prods) => {
        // Solo productos con stock > 0
        this.productosDisponibles = prods.filter(p => p.stock > 0);
        this.cargandoProductos = false;
      },
      error: () => {
        this.mensajeError = 'No se pudo cargar el catálogo de productos.';
        this.cargandoProductos = false;
      }
    });
  }

  alSeleccionarProducto(): void {
    this.productoSeleccionado = this.productosDisponibles.find(p => p.id === this.productoSeleccionadoId) || null;
    this.cantidadIngresada = 1;
  }

  puedeAgregarItem(): boolean {
    if (!this.productoSeleccionado) return false;
    if (this.cantidadIngresada <= 0) return false;
    if (this.cantidadIngresada > this.productoSeleccionado.stock) return false;
    return true;
  }

  agregarItem(): void {
    if (!this.productoSeleccionado || !this.puedeAgregarItem()) return;

    const existente = this.canasta.find(i => i.producto.id === this.productoSeleccionado!.id);
    if (existente) {
      const nuevaCantidad = existente.quantity + this.cantidadIngresada;
      if (nuevaCantidad > this.productoSeleccionado.stock) {
        alert(`No puede agregar más de ${this.productoSeleccionado.stock} unidades de este producto.`);
        return;
      }
      existente.quantity = nuevaCantidad;
      this.recalcularSubtotal(existente);
    } else {
      this.canasta.push({
        producto: this.productoSeleccionado,
        quantity: this.cantidadIngresada,
        subtotal: this.productoSeleccionado.price * this.cantidadIngresada
      });
    }

    this.productoSeleccionadoId = null;
    this.productoSeleccionado = null;
    this.cantidadIngresada = 1;
  }

  recalcularSubtotal(item: ItemCanasta): void {
    if (item.quantity <= 0) item.quantity = 1;
    if (item.quantity > item.producto.stock) item.quantity = item.producto.stock;
    item.subtotal = item.producto.price * item.quantity;
  }

  eliminarItem(index: number): void {
    this.canasta.splice(index, 1);
  }

  limpiarCanasta(): void {
    this.canasta = [];
  }

  get totalEstimado(): number {
    return this.canasta.reduce((sum, item) => sum + item.subtotal, 0);
  }

  confirmarPedido(): void {
    if (this.canasta.length === 0) return;

    this.procesando = true;
    this.mensajeError = '';

    const payload: CrearPedido = {
      items: this.canasta.map(i => ({
        productId: i.producto.id,
        quantity: i.quantity
      }))
    };

    this.pedidosService.registrarPedido(payload).subscribe({
      next: (pedidoRegistrado) => {
        this.procesando = false;
        this.router.navigate(['/pedidos', pedidoRegistrado.id]);
      },
      error: (err) => {
        this.procesando = false;
        this.mensajeError = err.error?.detail || err.error?.title || 'Ocurrió un error al procesar el pedido.';
      }
    });
  }
}
