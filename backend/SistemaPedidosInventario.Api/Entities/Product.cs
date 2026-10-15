namespace SistemaPedidosInventario.Api.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool Active { get; set; } = true;

    // Concurrencia optimista para evitar sobreventas en accesos simultáneos
    public byte[]? RowVersion { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
