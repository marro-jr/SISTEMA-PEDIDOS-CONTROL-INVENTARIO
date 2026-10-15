import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { MovimientoStock } from '../models/inventario.model';
import { ProductoBajoStock } from '../models/producto.model';

@Injectable({
  providedIn: 'root'
})
export class InventarioService {
  private readonly baseUrl = `${environment.apiUrl}/inventario`;

  constructor(private http: HttpClient) {}

  obtenerMovimientos(productoId?: number, limite: number = 100): Observable<MovimientoStock[]> {
    let params = new HttpParams().set('limite', limite);
    if (productoId !== undefined && productoId !== null) {
      params = params.set('productoId', productoId);
    }
    return this.http.get<MovimientoStock[]>(`${this.baseUrl}/movimientos`, { params });
  }

  obtenerProductosBajoStock(umbral: number = 10): Observable<ProductoBajoStock[]> {
    const params = new HttpParams().set('umbral', umbral);
    return this.http.get<ProductoBajoStock[]>(`${this.baseUrl}/bajo-stock`, { params });
  }
}
