export interface DetallePedido {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface Pedido {
  id: number;
  createdAt: string;
  total: number;
  status: string;
  detalle: DetallePedido[];
}

export interface ItemPedido {
  productId: number;
  quantity: number;
}

export interface CrearPedido {
  items: ItemPedido[];
}
