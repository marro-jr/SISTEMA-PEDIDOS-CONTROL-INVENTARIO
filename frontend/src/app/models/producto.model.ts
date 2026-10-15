export interface Producto {
  id: number;
  name: string;
  description?: string;
  price: number;
  stock: number;
  active: boolean;
}

export interface CrearProducto {
  name: string;
  description?: string;
  price: number;
  stock: number;
}

export interface ActualizarProducto {
  name: string;
  description?: string;
  price: number;
  stock: number;
  active: boolean;
}

export interface ProductoBajoStock {
  id: number;
  name: string;
  price: number;
  stock: number;
  active: boolean;
  umbral: number;
}
