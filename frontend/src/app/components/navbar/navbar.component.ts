import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark sticky-top shadow-sm">
      <div class="container-fluid px-4">
        <a class="navbar-brand d-flex align-items-center gap-2 fw-semibold" routerLink="/">
          <i class="bi bi-boxes text-primary fs-4"></i>
          <span>Control Stock & Pedidos</span>
        </a>
        <button
          class="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#navbarContenido"
          aria-controls="navbarContenido"
          aria-expanded="false"
          aria-label="Alternar navegación"
        >
          <span class="navbar-toggler-icon"></span>
        </button>

        <div class="collapse navbar-collapse" id="navbarContenido">
          <ul class="navbar-nav me-auto mb-2 mb-lg-0">
            <li class="nav-item">
              <a class="nav-link" routerLink="/dashboard" routerLinkActive="active">
                <i class="bi bi-speedometer2 me-1"></i> Dashboard
              </a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/productos" routerLinkActive="active">
                <i class="bi bi-box-seam me-1"></i> Productos
              </a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/pedidos" routerLinkActive="active">
                <i class="bi bi-receipt me-1"></i> Pedidos
              </a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/pedidos/nuevo" routerLinkActive="active">
                <i class="bi bi-cart-plus me-1"></i> Nuevo Pedido
              </a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/inventario" routerLinkActive="active">
                <i class="bi bi-clipboard2-data me-1"></i> Inventario
              </a>
            </li>
          </ul>

          <div class="d-flex align-items-center text-white-50 small">
            <span class="badge bg-secondary me-2">ASP.NET Core 10 + Angular</span>
            <span>Ambiente Interno</span>
          </div>
        </div>
      </div>
    </nav>
  `,
  styles: [`
    .nav-link.active {
      color: #fff !important;
      font-weight: 600;
    }
  `]
})
export class NavbarComponent {}
