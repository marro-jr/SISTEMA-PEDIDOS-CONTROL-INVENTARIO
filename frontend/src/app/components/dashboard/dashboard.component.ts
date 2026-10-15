import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ProductosService } from '../../services/productos.service';
import { PedidosService } from '../../services/pedidos.service';
import { InventarioService } from '../../services/inventario.service';
import { Producto, ProductoBajoStock } from '../../models/producto.model';
import { Pedido } from '../../models/pedido.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container-fluid py-4 px-4">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h2 class="fw-bold mb-1">Panel de Control de Inventario y Pedidos</h2>
          <p class="text-muted mb-0">Visión general operativa, existencias críticas y últimos pedidos registrados.</p>
        </div>
        <div class="d-flex gap-2">
          <a routerLink="/pedidos/nuevo" class="btn btn-primary d-flex align-items-center gap-2">
            <i class="bi bi-cart-plus"></i> Registrar Pedido
          </a>
          <a routerLink="/productos/nuevo" class="btn btn-outline-secondary d-flex align-items-center gap-2">
            <i class="bi bi-plus-circle"></i> Nuevo Producto
          </a>
        </div>
      </div>

      <!-- Spinner de carga -->
      <div *ngIf="cargando" class="text-center py-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Cargando datos...</span>
        </div>
        <p class="text-muted mt-2">Cargando métricas del sistema...</p>
      </div>

      <!-- Alerta de error -->
      <div *ngIf="mensajeError" class="alert alert-danger d-flex align-items-center gap-2" role="alert">
        <i class="bi bi-exclamation-triangle-fill fs-5"></i>
        <div>{{ mensajeError }}</div>
      </div>

      <div *ngIf="!cargando && !mensajeError">
        <!-- Tarjetas de métricas -->
        <div class="row g-3 mb-4">
          <div class="col-12 col-sm-6 col-xl-3">
            <div class="card h-100 border-start border-primary border-4">
              <div class="card-body">
                <div class="d-flex justify-content-between align-items-center">
                  <div>
                    <h6 class="text-muted text-uppercase fw-semibold mb-1 small">Total Productos</h6>
                    <h3 class="fw-bold mb-0">{{ totalProductos }}</h3>
                    <small class="text-muted">{{ productosActivos }} activos</small>
                  </div>
                  <div class="bg-primary bg-opacity-10 p-3 rounded-circle text-primary">
                    <i class="bi bi-box-seam fs-3"></i>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="col-12 col-sm-6 col-xl-3">
            <div class="card h-100 border-start border-warning border-4">
              <div class="card-body">
                <div class="d-flex justify-content-between align-items-center">
                  <div>
                    <h6 class="text-muted text-uppercase fw-semibold mb-1 small">Stock Crítico (≤ 10)</h6>
                    <h3 class="fw-bold mb-0 text-warning">{{ productosBajoStock.length }}</h3>
                    <small class="text-muted">Requieren reposición</small>
                  </div>
                  <div class="bg-warning bg-opacity-10 p-3 rounded-circle text-warning">
                    <i class="bi bi-exclamation-triangle fs-3"></i>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="col-12 col-sm-6 col-xl-3">
            <div class="card h-100 border-start border-success border-4">
              <div class="card-body">
                <div class="d-flex justify-content-between align-items-center">
                  <div>
                    <h6 class="text-muted text-uppercase fw-semibold mb-1 small">Total Pedidos</h6>
                    <h3 class="fw-bold mb-0">{{ totalPedidos }}</h3>
                    <small class="text-muted">Procesados con éxito</small>
                  </div>
                  <div class="bg-success bg-opacity-10 p-3 rounded-circle text-success">
                    <i class="bi bi-receipt fs-3"></i>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="col-12 col-sm-6 col-xl-3">
            <div class="card h-100 border-start border-info border-4">
              <div class="card-body">
                <div class="d-flex justify-content-between align-items-center">
                  <div>
                    <h6 class="text-muted text-uppercase fw-semibold mb-1 small">Ventas Totales</h6>
                    <h3 class="fw-bold mb-0 text-primary">S/ {{ totalVendido | number:'1.2-2' }}</h3>
                    <small class="text-muted">Monto acumulado</small>
                  </div>
                  <div class="bg-info bg-opacity-10 p-3 rounded-circle text-info">
                    <i class="bi bi-currency-dollar fs-3"></i>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="row g-4">
          <!-- Alerta de productos bajo stock -->
          <div class="col-12 col-lg-5">
            <div class="card h-100">
              <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                <h5 class="fw-bold mb-0 d-flex align-items-center gap-2">
                  <i class="bi bi-shield-exclamation text-warning"></i>
                  Alerta de Existencias Bajas
                </h5>
                <a routerLink="/inventario" class="btn btn-sm btn-outline-primary">Ver inventario</a>
              </div>
              <div class="card-body p-0">
                <div *ngIf="productosBajoStock.length === 0" class="text-center py-4 text-muted">
                  <i class="bi bi-check-circle text-success fs-2"></i>
                  <p class="mt-2 mb-0">Todas las existencias están por encima del umbral mínimo.</p>
                </div>
                <div *ngIf="productosBajoStock.length > 0" class="table-responsive">
                  <table class="table table-hover mb-0 align-middle">
                    <thead class="table-light">
                      <tr>
                        <th>Producto</th>
                        <th class="text-center">Stock</th>
                        <th class="text-end">Precio</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let prod of productosBajoStock">
                        <td>
                          <span class="fw-semibold">{{ prod.name }}</span>
                        </td>
                        <td class="text-center">
                          <span class="badge" [ngClass]="prod.stock <= 5 ? 'bg-danger' : 'bg-warning text-dark'">
                            {{ prod.stock }} unid.
                          </span>
                        </td>
                        <td class="text-end">S/ {{ prod.price | number:'1.2-2' }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>

          <!-- Últimos pedidos -->
          <div class="col-12 col-lg-7">
            <div class="card h-100">
              <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                <h5 class="fw-bold mb-0 d-flex align-items-center gap-2">
                  <i class="bi bi-clock-history text-primary"></i>
                  Últimos Pedidos Registrados
                </h5>
                <a routerLink="/pedidos" class="btn btn-sm btn-outline-primary">Ver todos</a>
              </div>
              <div class="card-body p-0">
                <div *ngIf="ultimosPedidos.length === 0" class="text-center py-4 text-muted">
                  <i class="bi bi-inbox fs-2"></i>
                  <p class="mt-2 mb-0">No se han registrado pedidos aún.</p>
                </div>
                <div *ngIf="ultimosPedidos.length > 0" class="table-responsive">
                  <table class="table table-hover mb-0 align-middle">
                    <thead class="table-light">
                      <tr>
                        <th># Pedido</th>
                        <th>Fecha</th>
                        <th class="text-center">Ítems</th>
                        <th class="text-end">Total</th>
                        <th class="text-center">Acción</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let ped of ultimosPedidos">
                        <td class="fw-semibold">#{{ ped.id }}</td>
                        <td>{{ ped.createdAt | date:'dd/MM/yyyy HH:mm' }}</td>
                        <td class="text-center">
                          <span class="badge bg-light text-dark border">{{ ped.detalle.length }}</span>
                        </td>
                        <td class="text-end fw-bold text-success">S/ {{ ped.total | number:'1.2-2' }}</td>
                        <td class="text-center">
                          <a [routerLink]="['/pedidos', ped.id]" class="btn btn-sm btn-outline-secondary">
                            Detalle
                          </a>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class DashboardComponent implements OnInit {
  cargando = true;
  mensajeError = '';

  totalProductos = 0;
  productosActivos = 0;
  totalPedidos = 0;
  totalVendido = 0;

  productosBajoStock: ProductoBajoStock[] = [];
  ultimosPedidos: Pedido[] = [];

  constructor(
    private productosService: ProductosService,
    private pedidosService: PedidosService,
    private inventarioService: InventarioService
  ) {}

  ngOnInit(): void {
    this.cargarDatosDashboard();
  }

  cargarDatosDashboard(): void {
    this.cargando = true;
    this.mensajeError = '';

    forkJoin({
      productos: this.productosService.obtenerProductos(),
      pedidos: this.pedidosService.obtenerPedidos(),
      bajoStock: this.inventarioService.obtenerProductosBajoStock(10)
    }).subscribe({
      next: ({ productos, pedidos, bajoStock }) => {
        this.totalProductos = productos.length;
        this.productosActivos = productos.filter(p => p.active).length;

        this.totalPedidos = pedidos.length;
        this.totalVendido = pedidos.reduce((acc, p) => acc + p.total, 0);

        this.productosBajoStock = bajoStock;
        this.ultimosPedidos = pedidos.slice(0, 5);
        this.cargando = false;
      },
      error: (err) => {
        this.mensajeError = 'Error al cargar los datos del dashboard. Verifique que la API esté activa.';
        this.cargando = false;
      }
    });
  }
}
