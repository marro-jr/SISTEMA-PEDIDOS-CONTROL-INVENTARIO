import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InventarioService } from '../../../services/inventario.service';
import { ProductosService } from '../../../services/productos.service';
import { MovimientoStock } from '../../../models/inventario.model';
import { ProductoBajoStock, Producto } from '../../../models/producto.model';

@Component({
  selector: 'app-vista-inventario',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="container-fluid py-4 px-4">
      <div class="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h2 class="fw-bold mb-1">Control de Inventario y Movimientos</h2>
          <p class="text-muted mb-0">Supervisión de stock crítico y auditoría de entradas y salidas de mercancía.</p>
        </div>
      </div>

      <!-- Pestañas de navegación -->
      <ul class="nav nav-pills mb-4">
        <li class="nav-item">
          <button
            class="nav-link d-flex align-items-center gap-2"
            [class.active]="seccionActiva === 'alertas'"
            (click)="cambiarSeccion('alertas')"
          >
            <i class="bi bi-exclamation-triangle"></i>
            Productos Bajo Stock
            <span class="badge bg-warning text-dark">{{ productosBajoStock.length }}</span>
          </button>
        </li>
        <li class="nav-item">
          <button
            class="nav-link d-flex align-items-center gap-2"
            [class.active]="seccionActiva === 'movimientos'"
            (click)="cambiarSeccion('movimientos')"
          >
            <i class="bi bi-clock-history"></i>
            Movimientos y Auditoría
          </button>
        </li>
      </ul>

      <!-- Sección 1: Productos con Bajo Stock -->
      <div *ngIf="seccionActiva === 'alertas'">
        <div class="card mb-4">
          <div class="card-body">
            <div class="row align-items-center g-3">
              <div class="col-auto">
                <label class="form-label fw-semibold mb-0">Umbral de Alerta:</label>
              </div>
              <div class="col-auto">
                <select class="form-select form-select-sm" [(ngModel)]="umbralSeleccionado" (change)="cargarBajoStock()">
                  <option [ngValue]="5">Existencias ≤ 5 unidades (Crítico)</option>
                  <option [ngValue]="10">Existencias ≤ 10 unidades (Estándar)</option>
                  <option [ngValue]="15">Existencias ≤ 15 unidades (Preventivo)</option>
                  <option [ngValue]="20">Existencias ≤ 20 unidades (Ampliación)</option>
                </select>
              </div>
              <div class="col text-muted small text-end">
                Se muestran productos activos con existencia menor o igual al umbral seleccionado.
              </div>
            </div>
          </div>
        </div>

        <div *ngIf="cargandoAlertas" class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
          <p class="text-muted mt-2">Consultando existencias...</p>
        </div>

        <div *ngIf="!cargandoAlertas" class="card shadow-sm">
          <div class="table-responsive">
            <table class="table table-hover mb-0 align-middle">
              <thead class="table-light">
                <tr>
                  <th>ID</th>
                  <th>Producto</th>
                  <th class="text-end">Precio</th>
                  <th class="text-center">Existencia Actual</th>
                  <th class="text-center">Nivel de Riesgo</th>
                  <th class="text-center">Acción</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngIf="productosBajoStock.length === 0">
                  <td colspan="6" class="text-center py-4 text-muted">
                    <i class="bi bi-check-circle text-success fs-3 d-block mb-1"></i>
                    No hay productos con existencias por debajo del umbral de {{ umbralSeleccionado }} unidades.
                  </td>
                </tr>
                <tr *ngFor="let item of productosBajoStock">
                  <td class="text-muted fw-semibold">#{{ item.id }}</td>
                  <td class="fw-semibold">{{ item.name }}</td>
                  <td class="text-end">S/ {{ item.price | number:'1.2-2' }}</td>
                  <td class="text-center">
                    <span class="badge fs-6" [ngClass]="item.stock <= 5 ? 'bg-danger' : 'bg-warning text-dark'">
                      {{ item.stock }} unid.
                    </span>
                  </td>
                  <td class="text-center">
                    <span class="badge" [ngClass]="item.stock <= 5 ? 'bg-danger-subtle text-danger border border-danger' : 'bg-warning-subtle text-warning-emphasis border border-warning'">
                      {{ item.stock <= 5 ? 'Crítico - Urge reponer' : 'Atención recomendada' }}
                    </span>
                  </td>
                  <td class="text-center">
                    <a [routerLink]="['/productos/editar', item.id]" class="btn btn-sm btn-outline-primary">
                      Ajustar Stock
                    </a>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Sección 2: Movimientos de Stock -->
      <div *ngIf="seccionActiva === 'movimientos'">
        <div class="card mb-4">
          <div class="card-body">
            <div class="row g-3 align-items-center">
              <div class="col-12 col-md-5">
                <label class="form-label small fw-semibold mb-1">Filtrar por Producto:</label>
                <select class="form-select form-select-sm" [(ngModel)]="filtroProductoId" (change)="cargarMovimientos()">
                  <option [ngValue]="null">-- Todos los productos --</option>
                  <option *ngFor="let p of catalogo" [ngValue]="p.id">{{ p.name }}</option>
                </select>
              </div>
              <div class="col-12 col-md-3">
                <label class="form-label small fw-semibold mb-1">Límite de registros:</label>
                <select class="form-select form-select-sm" [(ngModel)]="limiteMovimientos" (change)="cargarMovimientos()">
                  <option [ngValue]="50">Últimos 50 movimientos</option>
                  <option [ngValue]="100">Últimos 100 movimientos</option>
                  <option [ngValue]="200">Últimos 200 movimientos</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        <div *ngIf="cargandoMovimientos" class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
          <p class="text-muted mt-2">Cargando movimientos de auditoría...</p>
        </div>

        <div *ngIf="!cargandoMovimientos" class="card shadow-sm">
          <div class="table-responsive">
            <table class="table table-hover mb-0 align-middle">
              <thead class="table-light">
                <tr>
                  <th style="width: 80px;">ID</th>
                  <th>Fecha y Hora</th>
                  <th>Producto</th>
                  <th class="text-center">Tipo de Movimiento</th>
                  <th class="text-center">Variación</th>
                  <th class="text-center">Pedido Asociado</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngIf="movimientos.length === 0">
                  <td colspan="6" class="text-center py-4 text-muted">
                    No se encontraron registros de movimiento.
                  </td>
                </tr>
                <tr *ngFor="let mov of movimientos">
                  <td class="text-muted">#{{ mov.id }}</td>
                  <td>{{ mov.createdAt | date:'dd/MM/yyyy HH:mm:ss' }}</td>
                  <td class="fw-semibold">{{ mov.productName }}</td>
                  <td class="text-center">
                    <span
                      class="badge"
                      [ngClass]="{
                        'bg-info text-dark': mov.movementType === 'INICIAL',
                        'bg-danger': mov.movementType === 'VENTA_PEDIDO',
                        'bg-primary': mov.movementType === 'AJUSTE'
                      }"
                    >
                      {{ mov.movementType }}
                    </span>
                  </td>
                  <td class="text-center fw-bold">
                    <span [ngClass]="mov.quantity >= 0 ? 'text-success' : 'text-danger'">
                      {{ mov.quantity > 0 ? '+' + mov.quantity : mov.quantity }}
                    </span>
                  </td>
                  <td class="text-center">
                    <a *ngIf="mov.orderId" [routerLink]="['/pedidos', mov.orderId]" class="btn btn-sm btn-outline-secondary">
                      #{{ mov.orderId }}
                    </a>
                    <span *ngIf="!mov.orderId" class="text-muted">—</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  `
})
export class VistaInventarioComponent implements OnInit {
  seccionActiva: 'alertas' | 'movimientos' = 'alertas';

  productosBajoStock: ProductoBajoStock[] = [];
  cargandoAlertas = true;
  umbralSeleccionado = 10;

  movimientos: MovimientoStock[] = [];
  catalogo: Producto[] = [];
  cargandoMovimientos = false;
  filtroProductoId: number | null = null;
  limiteMovimientos = 100;

  constructor(
    private inventarioService: InventarioService,
    private productosService: ProductosService
  ) {}

  ngOnInit(): void {
    this.cargarBajoStock();
    this.cargarCatalogo();
  }

  cambiarSeccion(seccion: 'alertas' | 'movimientos'): void {
    this.seccionActiva = seccion;
    if (seccion === 'movimientos' && this.movimientos.length === 0) {
      this.cargarMovimientos();
    }
  }

  cargarBajoStock(): void {
    this.cargandoAlertas = true;
    this.inventarioService.obtenerProductosBajoStock(this.umbralSeleccionado).subscribe({
      next: (datos) => {
        this.productosBajoStock = datos;
        this.cargandoAlertas = false;
      },
      error: () => {
        this.cargandoAlertas = false;
      }
    });
  }

  cargarCatalogo(): void {
    this.productosService.obtenerProductos().subscribe({
      next: (prods) => {
        this.catalogo = prods;
      }
    });
  }

  cargarMovimientos(): void {
    this.cargandoMovimientos = true;
    this.inventarioService.obtenerMovimientos(this.filtroProductoId ?? undefined, this.limiteMovimientos).subscribe({
      next: (datos) => {
        this.movimientos = datos;
        this.cargandoMovimientos = false;
      },
      error: () => {
        this.cargandoMovimientos = false;
      }
    });
  }
}
