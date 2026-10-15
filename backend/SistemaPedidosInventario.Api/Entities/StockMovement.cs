namespace SistemaPedidosInventario.Api.Entities;

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty; // "INICIAL", "VENTA_PEDIDO", "AJUSTE"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? OrderId { get; set; }

    public Product? Product { get; set; }
    public Order? Order { get; set; }
}
