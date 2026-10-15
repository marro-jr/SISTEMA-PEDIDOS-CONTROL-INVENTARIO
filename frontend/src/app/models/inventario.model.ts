export interface MovimientoStock {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  movementType: string;
  createdAt: string;
  orderId?: number | null;
}
