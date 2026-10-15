import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PedidosService } from '../../../services/pedidos.service';
import { Pedido } from '../../../models/pedido.model';

@Component({
  selector: 'app-lista-pedidos',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container-fluid py-4 px-4">
      <div class="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
        <div>
          <h2 class="fw-bold mb-1">Historial de Pedidos</h2>
          <p class="text-muted mb-0">Registro cronológico de pedidos y ventas realizadas.</p>
        </div>
        <a routerLink="/pedidos/nuevo" class="btn btn-primary d-flex align-items-center gap-2">
          <i class="bi bi-cart-plus"></i> Registrar Nuevo Pedido
        </a>
      </div>

      <!-- Spinner -->
      <div *ngIf="cargando" class="text-center py-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Cargando...</span>
        </div>
        <p class="text-muted mt-2">Cargando historial de pedidos...</p>
      </div>

      <!-- Error -->
      <div *ngIf="mensajeError" class="alert alert-danger" role="alert">
        <i class="bi bi-exclamation-triangle-fill me-2"></i> {{ mensajeError }}
      </div>

      <!-- Tabla de pedidos -->
      <div *ngIf="!cargando && !mensajeError" class="card shadow-sm">
        <div class="card-body p-0">
          <div *ngIf="pedidos.length === 0" class="text-center py-5 text-muted">
            <i class="bi bi-inbox fs-1"></i>
            <h5 class="mt-3">No hay pedidos registrados</h5>
            <p class="mb-3">Comience registrando el primer pedido desde el botón superior.</p>
            <a routerLink="/pedidos/nuevo" class="btn btn-primary">Registrar Primer Pedido</a>
          </div>

          <div *ngIf="pedidos.length > 0" class="table-responsive">
            <table class="table table-hover mb-0 align-middle">
              <thead class="table-light">
                <tr>
                  <th style="width: 100px;"># Pedido</th>
                  <th>Fecha de Registro</th>
                  <th class="text-center" style="width: 140px;">Ítems</th>
                  <th class="text-end" style="width: 160px;">Monto Total</th>
                  <th class="text-center" style="width: 140px;">Estado</th>
                  <th class="text-center" style="width: 130px;">Acciones</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let ped of pedidos">
                  <td class="fw-bold text-primary">#{{ ped.id }}</td>
                  <td>
                    <div>{{ ped.createdAt | date:'dd/MM/yyyy HH:mm:ss' }}</div>
                  </td>
                  <td class="text-center">
                    <span class="badge bg-light text-dark border">
                      {{ ped.detalle.length }} {{ ped.detalle.length === 1 ? 'producto' : 'productos' }}
                    </span>
                  </td>
                  <td class="text-end fw-bold text-success fs-6">
                    S/ {{ ped.total | number:'1.2-2' }}
                  </td>
                  <td class="text-center">
                    <span class="badge bg-success-subtle text-success border border-success">
                      {{ ped.status }}
                    </span>
                  </td>
                  <td class="text-center">
                    <a [routerLink]="['/pedidos', ped.id]" class="btn btn-sm btn-outline-primary d-inline-flex align-items-center gap-1">
                      <i class="bi bi-eye"></i> Detalle
                    </a>
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
export class ListaPedidosComponent implements OnInit {
  pedidos: Pedido[] = [];
  cargando = true;
  mensajeError = '';

  constructor(private pedidosService: PedidosService) {}

  ngOnInit(): void {
    this.cargarPedidos();
  }

  cargarPedidos(): void {
    this.cargando = true;
    this.pedidosService.obtenerPedidos().subscribe({
      next: (datos) => {
        this.pedidos = datos;
        this.cargando = false;
      },
      error: () => {
        this.mensajeError = 'No se pudieron cargar los pedidos.';
        this.cargando = false;
      }
    });
  }
}
