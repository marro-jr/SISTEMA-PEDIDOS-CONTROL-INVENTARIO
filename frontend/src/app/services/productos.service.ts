import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Producto, CrearProducto, ActualizarProducto } from '../models/producto.model';

@Injectable({
  providedIn: 'root'
})
export class ProductosService {
  private readonly baseUrl = `${environment.apiUrl}/productos`;

  constructor(private http: HttpClient) {}

  obtenerProductos(soloActivos?: boolean): Observable<Producto[]> {
    let params = new HttpParams();
    if (soloActivos !== undefined) {
      params = params.set('soloActivos', soloActivos);
    }
    return this.http.get<Producto[]>(this.baseUrl, { params });
  }

  obtenerProductoPorId(id: number): Observable<Producto> {
    return this.http.get<Producto>(`${this.baseUrl}/${id}`);
  }

  crearProducto(producto: CrearProducto): Observable<Producto> {
    return this.http.post<Producto>(this.baseUrl, producto);
  }

  actualizarProducto(id: number, producto: ActualizarProducto): Observable<Producto> {
    return this.http.put<Producto>(`${this.baseUrl}/${id}`, producto);
  }

  inactivarProducto(id: number): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/inactivar`, {});
  }
}
