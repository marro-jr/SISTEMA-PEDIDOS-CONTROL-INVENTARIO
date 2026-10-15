import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductosService } from '../../../services/productos.service';
import { Producto } from '../../../models/producto.model';

@Component({
  selector: 'app-lista-productos',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="container-fluid py-4 px-4">
      <div class="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h2 class="fw-bold mb-1">Catálogo de Productos</h2>
          <p class="text-muted mb-0">Gestión de productos, precios y existencias en inventario.</p>
        </div>
        <a routerLink="/productos/nuevo" class="btn btn-primary d-flex align-items-center gap-2">
          <i class="bi bi-plus-lg"></i> Registrar Producto
        </a>
      </div>

      <!-- Filtros de búsqueda -->
      <div class="card mb-4">
        <div class="card-body">
          <div class="row g-3">
            <div class="col-12 col-md-6 col-lg-4">
              <div class="input-group">
                <span class="input-group-text bg-white"><i class="bi bi-search"></i></span>
                <input
                  type="text"
                  class="form-control"
                  placeholder="Buscar producto por nombre..."
                  [(ngModel)]="terminoBusqueda"
                  (input)="filtrarProductos()"
                />
              </div>
            </div>
            <div class="col-12 col-md-6 col-lg-3">
              <select class="form-select" [(ngModel)]="filtroEstado" (change)="filtrarProductos()">
                <option value="todos">Todos los estados</option>
                <option value="activos">Solo activos</option>
                <option value="inactivos">Solo inactivos</option>
              </select>
            </div>
            <div class="col-12 col-lg-5 text-lg-end text-muted small align-self-center">
              Mostrando {{ productosFiltrados.length }} de {{ productos.length }} productos
            </div>
          </div>
        </div>
      </div>

      <!-- Notificación de éxito o advertencia -->
      <div *ngIf="mensajeExito" class="alert alert-success alert-dismissible fade show" role="alert">
        <i class="bi bi-check-circle-fill me-2"></i> {{ mensajeExito }}
        <button type="button" class="btn-close" (click)="mensajeExito = ''" aria-label="Cerrar"></button>
      </div>

      <!-- Spinner -->
      <div *ngIf="cargando" class="text-center py-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Cargando...</span>
        </div>
        <p class="text-muted mt-2">Cargando productos...</p>
      </div>

      <!-- Tabla de productos -->
      <div *ngIf="!cargando" class="card shadow-sm">
        <div class="table-responsive">
          <table class="table table-hover mb-0 align-middle">
            <thead class="table-light">
              <tr>
                <th style="width: 70px;">ID</th>
                <th>Nombre y Descripción</th>
                <th class="text-end" style="width: 130px;">Precio</th>
                <th class="text-center" style="width: 140px;">Existencias</th>
                <th class="text-center" style="width: 120px;">Estado</th>
                <th class="text-center" style="width: 160px;">Acciones</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngIf="productosFiltrados.length === 0">
                <td colspan="6" class="text-center py-4 text-muted">
                  No se encontraron productos con los criterios especificados.
                </td>
              </tr>
              <tr *ngFor="let prod of productosFiltrados">
                <td class="text-muted fw-semibold">#{{ prod.id }}</td>
                <td>
                  <div class="fw-semibold">{{ prod.name }}</div>
                  <small class="text-muted">{{ prod.description || 'Sin descripción' }}</small>
                </td>
                <td class="text-end fw-semibold">S/ {{ prod.price | number:'1.2-2' }}</td>
                <td class="text-center">
                  <span
                    class="badge"
                    [ngClass]="{
                      'bg-danger': prod.stock <= 5,
                      'bg-warning text-dark': prod.stock > 5 && prod.stock <= 10,
                      'bg-success': prod.stock > 10
                    }"
                  >
                    {{ prod.stock }} unid.
                  </span>
                </td>
                <td class="text-center">
                  <span class="badge" [ngClass]="prod.active ? 'bg-success-subtle text-success border border-success' : 'bg-secondary-subtle text-secondary border border-secondary'">
                    {{ prod.active ? 'Activo' : 'Inactivo' }}
                  </span>
                </td>
                <td class="text-center">
                  <div class="btn-group btn-group-sm">
                    <a [routerLink]="['/productos/editar', prod.id]" class="btn btn-outline-primary" title="Editar información">
                      <i class="bi bi-pencil"></i>
                    </a>
                    <button
                      *ngIf="prod.active"
                      class="btn btn-outline-warning"
                      (click)="confirmarInactivar(prod)"
                      title="Dar de baja lógica"
                    >
                      <i class="bi bi-slash-circle"></i>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `
})
export class ListaProductosComponent implements OnInit {
  productos: Producto[] = [];
  productosFiltrados: Producto[] = [];
  cargando = true;
  terminoBusqueda = '';
  filtroEstado = 'todos';
  mensajeExito = '';

  constructor(private productosService: ProductosService) {}

  ngOnInit(): void {
    this.cargarProductos();
  }

  cargarProductos(): void {
    this.cargando = true;
    this.productosService.obtenerProductos().subscribe({
      next: (datos) => {
        this.productos = datos;
        this.filtrarProductos();
        this.cargando = false;
      },
      error: () => {
        this.cargando = false;
      }
    });
  }

  filtrarProductos(): void {
    this.productosFiltrados = this.productos.filter((p) => {
      const coincideNombre = p.name.toLowerCase().includes(this.terminoBusqueda.toLowerCase()) ||
        (p.description && p.description.toLowerCase().includes(this.terminoBusqueda.toLowerCase()));

      if (!coincideNombre) return false;

      if (this.filtroEstado === 'activos') return p.active;
      if (this.filtroEstado === 'inactivos') return !p.active;
      return true;
    });
  }

  confirmarInactivar(producto: Producto): void {
    if (confirm(`¿Está seguro de inactivar el producto "${producto.name}"? Los pedidos existentes preservarán su historial.`)) {
      this.productosService.inactivarProducto(producto.id).subscribe({
        next: () => {
          this.mensajeExito = `Producto "${producto.name}" inactivado correctamente.`;
          this.cargarProductos();
        }
      });
    }
  }
}
