import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { PedidosService } from '../../../services/pedidos.service';
import { Pedido } from '../../../models/pedido.model';

@Component({
  selector: 'app-detalle-pedido',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container py-4">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
          <a routerLink="/pedidos" class="btn btn-outline-secondary btn-sm mb-2">
            <i class="bi bi-arrow-left"></i> Volver a Pedidos
          </a>
          <h2 class="fw-bold mb-0">Detalle de Pedido #{{ pedidoId }}</h2>
        </div>
      </div>

      <!-- Spinner -->
      <div *ngIf="cargando" class="text-center py-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Cargando...</span>
        </div>
        <p class="text-muted mt-2">Cargando detalles del pedido...</p>
      </div>

      <!-- Error -->
      <div *ngIf="mensajeError" class="alert alert-danger" role="alert">
        <i class="bi bi-exclamation-triangle-fill me-2"></i> {{ mensajeError }}
      </div>

      <div *ngIf="!cargando && pedido" class="row g-4">
        <!-- Tarjeta de resumen -->
        <div class="col-12 col-md-4">
          <div class="card shadow-sm h-100">
            <div class="card-header bg-white py-3">
              <h5 class="fw-bold mb-0">Información General</h5>
            </div>
            <div class="card-body">
              <ul class="list-group list-group-flush">
                <li class="list-group-item px-0 d-flex justify-content-between">
                  <span class="text-muted">Número de Pedido:</span>
                  <span class="fw-bold">#{{ pedido.id }}</span>
                </li>
                <li class="list-group-item px-0 d-flex justify-content-between">
                  <span class="text-muted">Fecha y Hora:</span>
                  <span>{{ pedido.createdAt | date:'dd/MM/yyyy HH:mm:ss' }}</span>
                </li>
                <li class="list-group-item px-0 d-flex justify-content-between">
                  <span class="text-muted">Estado del Pedido:</span>
                  <span class="badge bg-success-subtle text-success border border-success">
                    {{ pedido.status }}
                  </span>
                </li>
                <li class="list-group-item px-0 d-flex justify-content-between align-items-center pt-3 border-top">
                  <span class="fw-bold fs-5">Monto Total:</span>
                  <span class="fw-bold fs-4 text-success">S/ {{ pedido.total | number:'1.2-2' }}</span>
                </li>
              </ul>

              <div class="alert alert-info mt-3 small mb-0">
                <i class="bi bi-info-circle me-1"></i>
                Los precios unitarios reflejan el valor congelado en el momento exacto de la compra.
              </div>
            </div>
          </div>
        </div>

        <!-- Tabla de líneas de pedido -->
        <div class="col-12 col-md-8">
          <div class="card shadow-sm h-100">
            <div class="card-header bg-white py-3">
              <h5 class="fw-bold mb-0">Artículos Comprados ({{ pedido.detalle.length }})</h5>
            </div>
            <div class="card-body p-0">
              <div class="table-responsive">
                <table class="table table-hover mb-0 align-middle">
                  <thead class="table-light">
                    <tr>
                      <th style="width: 60px;">#</th>
                      <th>Producto</th>
                      <th class="text-center" style="width: 110px;">Cantidad</th>
                      <th class="text-end" style="width: 140px;">Precio Unitario</th>
                      <th class="text-end" style="width: 140px;">Subtotal</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let item of pedido.detalle; let i = index">
                      <td class="text-muted">{{ i + 1 }}</td>
                      <td>
                        <span class="fw-semibold">{{ item.productName }}</span>
                        <div class="small text-muted">ID Producto: {{ item.productId }}</div>
                      </td>
                      <td class="text-center">
                        <span class="badge bg-light text-dark border px-2 py-1">
                          {{ item.quantity }}
                        </span>
                      </td>
                      <td class="text-end">S/ {{ item.unitPrice | number:'1.2-2' }}</td>
                      <td class="text-end fw-bold">S/ {{ item.subtotal | number:'1.2-2' }}</td>
                    </tr>
                  </tbody>
                  <tfoot class="table-light">
                    <tr>
                      <td colspan="4" class="text-end fw-bold fs-6">Total Calculado:</td>
                      <td class="text-end fw-bold fs-6 text-success">S/ {{ pedido.total | number:'1.2-2' }}</td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class DetallePedidoComponent implements OnInit {
  pedidoId = 0;
  pedido: Pedido | null = null;
  cargando = true;
  mensajeError = '';

  constructor(
    private route: ActivatedRoute,
    private pedidosService: PedidosService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.pedidoId = Number(id);
      this.cargarDetalle(this.pedidoId);
    }
  }

  cargarDetalle(id: number): void {
    this.cargando = true;
    this.pedidosService.obtenerPedidoPorId(id).subscribe({
      next: (datos) => {
        this.pedido = datos;
        this.cargando = false;
      },
      error: () => {
        this.mensajeError = `No se pudo encontrar el pedido con ID #${id}.`;
        this.cargando = false;
      }
    });
  }
}
