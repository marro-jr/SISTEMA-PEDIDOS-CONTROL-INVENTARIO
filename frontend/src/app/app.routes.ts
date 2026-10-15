import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ListaProductosComponent } from './components/productos/lista-productos/lista-productos.component';
import { FormularioProductoComponent } from './components/productos/formulario-producto/formulario-producto.component';
import { ListaPedidosComponent } from './components/pedidos/lista-pedidos/lista-pedidos.component';
import { CrearPedidoComponent } from './components/pedidos/crear-pedido/crear-pedido.component';
import { DetallePedidoComponent } from './components/pedidos/detalle-pedido/detalle-pedido.component';
import { VistaInventarioComponent } from './components/inventario/vista-inventario/vista-inventario.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent, title: 'Dashboard | Control Stock' },
  { path: 'productos', component: ListaProductosComponent, title: 'Catálogo de Productos' },
  { path: 'productos/nuevo', component: FormularioProductoComponent, title: 'Registrar Producto' },
  { path: 'productos/editar/:id', component: FormularioProductoComponent, title: 'Editar Producto' },
  { path: 'pedidos', component: ListaPedidosComponent, title: 'Historial de Pedidos' },
  { path: 'pedidos/nuevo', component: CrearPedidoComponent, title: 'Registrar Pedido' },
  { path: 'pedidos/:id', component: DetallePedidoComponent, title: 'Detalle de Pedido' },
  { path: 'inventario', component: VistaInventarioComponent, title: 'Control de Inventario' },
  { path: '**', redirectTo: 'dashboard' }
];
